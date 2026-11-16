# Copyright (c) Files Community
# Licensed under the MIT License.

"""Prepares and publishes the AI review gate for a pull request.

prepare: decides whether the PR needs a review and writes the review context.
publish: posts the reviewer's comments and sets the labels and commit status.
error:   marks the commit status as errored after a failed run.

PR content is untrusted. This script never runs PR code; it only reads PR data
through the GitHub API and extracts the PR files with `git archive`.
"""

import datetime
import json
import os
import re
import shutil
import subprocess
import sys

REPO = os.environ["GITHUB_REPOSITORY"]
CONTEXT = os.path.join(os.environ.get("RUNNER_TEMP", "/tmp"), "ai-review")
META = os.path.join(CONTEXT, "meta.json")

STATUS_CONTEXT = "AI review"
BOT_LOGIN = "github-actions[bot]"
LABEL_PENDING = "needs - ai review"
LABEL_CHANGES = "changes requested"
LABEL_READY = "ready for review"
LABEL_SKIP = "skip ai review"
LABEL_COLORS = {LABEL_PENDING: "fbca04", LABEL_SKIP: "c5def5"}

EXEMPT_ASSOCIATIONS = {"OWNER", "MEMBER", "COLLABORATOR"}
EXEMPT_PERMISSIONS = {"admin", "maintain", "write"}

SUMMARY_MARKER = "<!-- ai-review-summary -->"
STATE_RE = re.compile(r"<!-- ai-review-state: (.*?) -->", re.S)
COMMENT_RE = re.compile(r"<!-- ai-review:(blocking|nit) -->")
SECRET_RE = re.compile(r"sk-ant-[A-Za-z0-9_-]{10,}|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}")

MAX_LINES = int(os.environ.get("AI_REVIEW_MAX_LINES") or 1500)
MAX_RUNS_PER_DAY = int(os.environ.get("AI_REVIEW_MAX_RUNS") or 6)
MAX_TEXT = 4000

FOOTER = (
    "_Automated first-pass review. A maintainer reviews the PR once nothing blocking is left. "
    "Maintainers can add the `skip ai review` label to bypass it or comment `/ai-review` to run it again._"
)


# GitHub helpers

def gh(method, path, body=None, check=True):
    if method != "GET" and os.environ.get("AI_REVIEW_DRY_RUN"):
        print(f"[dry run] {method} {path} {json.dumps(body) if body is not None else ''}")
        return {"id": 0}
    args = ["gh", "api", "-X", method, path, "-H", "Accept: application/vnd.github+json"]
    if body is not None:
        args += ["--input", "-"]
    result = subprocess.run(args, input=json.dumps(body) if body is not None else None, capture_output=True, text=True)
    if result.returncode != 0:
        if check:
            raise RuntimeError(f"gh api {method} {path} failed: {result.stderr.strip()}")
        return None
    return json.loads(result.stdout) if result.stdout.strip() else None


def gh_all(path):
    items, page = [], 1
    sep = "&" if "?" in path else "?"
    while True:
        batch = gh("GET", f"{path}{sep}per_page=100&page={page}")
        items += batch
        if len(batch) < 100:
            return items
        page += 1


def set_status(sha, state, description):
    run_url = f"{os.environ.get('GITHUB_SERVER_URL', 'https://github.com')}/{REPO}/actions/runs/{os.environ.get('GITHUB_RUN_ID', '')}"
    gh("POST", f"repos/{REPO}/statuses/{sha}", {
        "state": state,
        "context": STATUS_CONTEXT,
        "description": description[:140],
        "target_url": run_url,
    })


def ensure_labels():
    for name, color in LABEL_COLORS.items():
        gh("POST", f"repos/{REPO}/labels", {"name": name, "color": color}, check=False)


def add_label(number, label):
    gh("POST", f"repos/{REPO}/issues/{number}/labels", {"labels": [label]}, check=False)


def remove_label(number, label):
    gh("DELETE", f"repos/{REPO}/issues/{number}/labels/{label.replace(' ', '%20')}", check=False)


def write_output(**values):
    with open(os.environ.get("GITHUB_OUTPUT", os.devnull), "a", encoding="utf-8") as f:
        for key, value in values.items():
            f.write(f"{key}={value}\n")


def clip(text, limit=MAX_TEXT):
    text = text or ""
    return text if len(text) <= limit else text[:limit] + "\n[truncated]"


# State kept in the summary comment

def find_summary(number):
    for comment in gh_all(f"repos/{REPO}/issues/{number}/comments"):
        if comment["user"]["login"] == BOT_LOGIN and SUMMARY_MARKER in (comment.get("body") or ""):
            match = STATE_RE.search(comment["body"])
            state = json.loads(match.group(1)) if match else {}
            return comment, state
    return None, {}


def our_review_comments(number):
    comments = gh_all(f"repos/{REPO}/pulls/{number}/comments")
    ours = {c["id"]: c for c in comments if c["user"]["login"] == BOT_LOGIN and COMMENT_RE.search(c.get("body") or "")}
    replies = {}
    for c in comments:
        parent = c.get("in_reply_to_id")
        if parent in ours and c["user"]["login"] != BOT_LOGIN:
            replies.setdefault(parent, []).append(c)
    return ours, replies


def severity_of(comment):
    match = COMMENT_RE.search(comment.get("body") or "")
    return match.group(1) if match else "nit"


def strip_marker(body):
    return COMMENT_RE.sub("", body or "").strip()


# Classification

def is_exempt(pr):
    user = pr["user"]
    if user.get("type") == "Bot":
        return True
    if pr.get("author_association") in EXEMPT_ASSOCIATIONS:
        return True
    return has_write_access(user["login"])


def has_write_access(login):
    # Private org membership is not visible in author_association, so check repo permission too.
    data = gh("GET", f"repos/{REPO}/collaborators/{login}/permission", check=False)
    return bool(data) and data.get("permission") in EXEMPT_PERMISSIONS


def is_maintainer_comment(comment):
    return comment.get("author_association") in EXEMPT_ASSOCIATIONS or has_write_access(comment["user"]["login"])


def head_status_is_final(sha):
    data = gh("GET", f"repos/{REPO}/commits/{sha}/status", check=False) or {}
    for status in data.get("statuses", []):
        if status.get("context") == STATUS_CONTEXT:
            return status.get("state") in ("success", "failure")
    return False


def today():
    return datetime.datetime.now(datetime.timezone.utc).date().isoformat()


def prepare():
    event_name = os.environ["GITHUB_EVENT_NAME"]
    with open(os.environ["GITHUB_EVENT_PATH"], encoding="utf-8") as f:
        event = json.load(f)

    number = event["pull_request"]["number"] if "pull_request" in event else event["issue"]["number"]
    pr = gh("GET", f"repos/{REPO}/pulls/{number}")
    sha = pr["head"]["sha"]
    labels = {label["name"] for label in pr.get("labels", [])}
    write_output(number=number, sha=sha)

    def done(mode):
        write_output(mode=mode)
        print(f"mode={mode}")

    if pr["state"] != "open":
        return done("ignore")

    if is_exempt(pr):
        set_status(sha, "success", "Not required for maintainers")
        remove_label(number, LABEL_PENDING)
        return done("exempt")

    ensure_labels()
    summary, state = find_summary(number)

    if LABEL_SKIP in labels:
        set_status(sha, "success", "Skipped by a maintainer")
        remove_label(number, LABEL_PENDING)
        if state.get("result") == "changes_requested":
            remove_label(number, LABEL_CHANGES)
        return done("skip")

    if os.environ.get("HAS_API_KEY") != "true":
        set_status(sha, "success", "AI review is not configured")
        return done("ignore")

    forced = False
    if event_name == "issue_comment":
        comment = event["comment"]
        if comment["body"].strip().startswith("/ai-review"):
            if not is_maintainer_comment(comment):
                return done("ignore")
            forced = True
        elif comment["user"]["login"] == pr["user"]["login"]:
            # The author replied. Re-run only if open questions are waiting on them,
            # or if a newer push has not been reviewed yet.
            waiting = state.get("result") == "changes_requested" and state.get("questions")
            if not waiting and head_status_is_final(sha):
                return done("ignore")
        else:
            return done("ignore")

    if pr.get("draft") and not forced:
        set_status(sha, "pending", "Waiting until the PR is marked ready for review")
        return done("ignore")

    runs = state.get("runs", 0) if state.get("date") == today() else 0
    if runs >= MAX_RUNS_PER_DAY and not forced:
        set_status(sha, "pending", "Daily AI review limit reached. A maintainer can comment /ai-review")
        return done("ignore")

    add_label(number, LABEL_PENDING)
    if state.get("result") == "passed":
        remove_label(number, LABEL_READY)

    size = pr["additions"] + pr["deletions"]
    if size > MAX_LINES:
        text = (f"This PR changes {size} lines, which is more than can be reviewed in one go. "
            "Please split it into smaller PRs that each address one issue.")
        upsert_summary(number, summary, text, [], [], {**state, "date": today(), "runs": runs, "result": "changes_requested", "questions": []})
        add_label(number, LABEL_CHANGES)
        set_status(sha, "failure", "PR is too large to review, please split it")
        return done("too_large")

    set_status(sha, "pending", "AI review in progress")
    write_context(pr, number, sha, summary, state)
    with open(META, "w", encoding="utf-8") as f:
        json.dump({"number": number, "sha": sha, "author": pr["user"]["login"], "summary_id": summary["id"] if summary else None,
            "state": {**state, "date": today(), "runs": runs + 1}}, f)
    done("review")


# Review context

def write_context(pr, number, sha, summary, state):
    shutil.rmtree(CONTEXT, ignore_errors=True)
    os.makedirs(CONTEXT)

    linked = linked_issues(number)
    with open(os.path.join(CONTEXT, "pr.md"), "w", encoding="utf-8") as f:
        f.write(f"# {pr['title']}\n\nAuthor: {pr['user']['login']} ({pr.get('author_association')})\n")
        f.write(f"Base: {pr['base']['ref']}\n\n## Linked issues\n\n")
        f.write("\n".join(f"- #{i['number']} {i['title']} (labels: {', '.join(i['labels']) or 'none'})" for i in linked) or "None")
        f.write(f"\n\n## Description\n\n{clip(pr.get('body'))}\n")

    diff = subprocess.run(["gh", "pr", "diff", str(number), "-R", REPO], capture_output=True, text=True, check=True).stdout
    with open(os.path.join(CONTEXT, "diff.txt"), "w", encoding="utf-8") as f:
        f.write(annotate_diff(diff))

    extract_head(number)

    ours, replies = our_review_comments(number)
    with open(os.path.join(CONTEXT, "previous.md"), "w", encoding="utf-8") as f:
        if ours:
            f.write("## Your earlier comments\n\n")
            for cid, c in ours.items():
                where = f"{c['path']}:{c.get('line') or c.get('original_line')}" + (" (outdated)" if c.get("line") is None else "")
                f.write(f"### id {cid} ({severity_of(c)}) at {where}\n\n{strip_marker(c['body'])}\n\n")
                for r in replies.get(cid, []):
                    f.write(f"Reply from {r['user']['login']}:\n\n{clip(r['body'], 2000)}\n\n")
        if state.get("questions"):
            f.write("## Your open questions\n\n" + "\n".join(f"- {q}" for q in state["questions"]) + "\n\n")
        discussion = [c for c in gh_all(f"repos/{REPO}/issues/{number}/comments") if not summary or c["id"] != summary["id"]]
        if discussion:
            f.write("## PR discussion\n\n")
            for c in discussion[-30:]:
                f.write(f"From {c['user']['login']}:\n\n{clip(c['body'], 2000)}\n\n")

    with open(os.path.join(os.path.dirname(__file__), "prompt.md"), encoding="utf-8") as f:
        prompt = f.read().replace("{CONTEXT}", CONTEXT)
    with open(os.path.join(CONTEXT, "prompt.txt"), "w", encoding="utf-8") as f:
        f.write(prompt)


def linked_issues(number):
    owner, name = REPO.split("/")
    query = """query($owner:String!,$name:String!,$number:Int!){repository(owner:$owner,name:$name){pullRequest(number:$number){
        closingIssuesReferences(first:10){nodes{number title labels(first:20){nodes{name}}}}}}}"""
    result = subprocess.run(["gh", "api", "graphql", "-f", f"query={query}", "-f", f"owner={owner}", "-f", f"name={name}", "-F", f"number={number}"],
        capture_output=True, text=True)
    if result.returncode != 0:
        return []
    nodes = json.loads(result.stdout)["data"]["repository"]["pullRequest"]["closingIssuesReferences"]["nodes"]
    return [{"number": n["number"], "title": n["title"], "labels": [l["name"] for l in n["labels"]["nodes"]]} for n in nodes]


def annotate_diff(diff):
    out, new_line = [], 0
    for line in diff.splitlines():
        if line.startswith("@@"):
            match = re.match(r"@@ -\d+(?:,\d+)? \+(\d+)", line)
            new_line = int(match.group(1)) if match else 0
            out.append(line)
        elif line.startswith(("diff --git", "index ", "--- ", "+++ ", "new file", "deleted file", "similarity", "rename ", "old mode", "new mode", "Binary files")):
            out.append(line)
        elif line.startswith("-"):
            out.append(f"       {line}")
        elif line.startswith("\\"):
            out.append(line)
        else:
            out.append(f"R{new_line:<5} {line}")
            new_line += 1
    return "\n".join(out) + "\n"


def extract_head(number):
    head = os.path.join(CONTEXT, "head")
    os.makedirs(head)
    subprocess.run(["git", "fetch", "--no-tags", "--depth=1", "origin", f"refs/pull/{number}/head"], check=True, capture_output=True)
    archive = subprocess.run(["git", "archive", "--format=tar", "FETCH_HEAD"], check=True, capture_output=True).stdout
    subprocess.run(["tar", "-x", "-C", head], input=archive, check=True)
    # Drop agent configuration from the PR copy so it cannot steer the reviewer.
    for root, dirs, files in os.walk(head):
        for d in [d for d in dirs if d == ".claude"]:
            shutil.rmtree(os.path.join(root, d))
            dirs.remove(d)
        for name in files:
            if name in ("CLAUDE.md", "CLAUDE.local.md", ".mcp.json"):
                os.remove(os.path.join(root, name))


# Publishing

def load_result(path):
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    if isinstance(data, list):
        data = next((d for d in reversed(data) if d.get("type") == "result"), {})
    if data.get("is_error"):
        raise RuntimeError(f"Reviewer failed: {data.get('subtype')}")
    review = data.get("structured_output")
    if review is None:
        text = (data.get("result") or "").strip()
        text = re.sub(r"^```(?:json)?\s*|\s*```$", "", text)
        review = json.loads(text)
    for key in ("summary", "summary_blocking", "resolved_comment_ids", "comments", "questions"):
        if key not in review:
            raise RuntimeError(f"Reviewer output is missing '{key}'")
    return review


def check_secrets(review):
    text = json.dumps(review)
    key = os.environ.get("ANTHROPIC_API_KEY") or ""
    if SECRET_RE.search(text) or (key and key in text):
        raise RuntimeError("Reviewer output contains what looks like a secret; nothing was posted")


def upsert_summary(number, summary, text, questions, unplaced, state):
    parts = [SUMMARY_MARKER, f"**AI review:** {text.strip()}"]
    if questions:
        parts.append("Before this goes to a maintainer, please answer these in a comment, in your own words:\n\n"
            + "\n".join(f"{i}. {q.strip()}" for i, q in enumerate(questions, 1)))
    if unplaced:
        parts.append("\n".join(f"- `{c['path']}`: {c['body'].strip()}" for c in unplaced))
    parts.append(FOOTER)
    parts.append(f"<!-- ai-review-state: {json.dumps(state).replace('-->', '--\\u003e')} -->")
    body = "\n\n".join(parts)
    if summary:
        gh("PATCH", f"repos/{REPO}/issues/comments/{summary['id']}", {"body": body})
    else:
        gh("POST", f"repos/{REPO}/issues/{number}/comments", {"body": body})


def publish(result_path):
    with open(META, encoding="utf-8") as f:
        meta = json.load(f)
    number, sha = meta["number"], meta["sha"]

    review = load_result(result_path)
    check_secrets(review)

    ours, _ = our_review_comments(number)
    resolved = {i for i in review["resolved_comment_ids"] if i in ours}
    for cid in resolved:
        gh("DELETE", f"repos/{REPO}/pulls/comments/{cid}", check=False)
    open_blocking = sum(1 for cid, c in ours.items() if cid not in resolved and severity_of(c) == "blocking")

    unplaced = []
    for c in review["comments"][:15]:
        severity = "blocking" if c["severity"] == "blocking" else "nit"
        body = f"{c['body'].strip()}\n\n<!-- ai-review:{severity} -->"
        posted = gh("POST", f"repos/{REPO}/pulls/{number}/comments",
            {"body": body, "commit_id": sha, "path": c["path"], "line": c["line"], "side": "RIGHT"}, check=False)
        if not posted:
            unplaced.append(c)
        if severity == "blocking":
            open_blocking += 1

    questions = [q for q in review["questions"] if q.strip()][:3]
    passed = open_blocking == 0 and not questions and not review["summary_blocking"]

    _, current_state = find_summary(number)
    state = {**meta["state"], "result": "passed" if passed else "changes_requested", "questions": questions}
    summary = {"id": meta["summary_id"]} if meta["summary_id"] else None
    upsert_summary(number, summary, review["summary"], questions, unplaced, state)

    remove_label(number, LABEL_PENDING)
    if passed:
        if current_state.get("result") == "changes_requested":
            remove_label(number, LABEL_CHANGES)
        add_label(number, LABEL_READY)
        set_status(sha, "success", "No blocking issues found")
    else:
        add_label(number, LABEL_CHANGES)
        reasons = []
        if open_blocking:
            reasons.append(f"{open_blocking} blocking comment{'s' if open_blocking != 1 else ''}")
        if review["summary_blocking"]:
            reasons.append("issues in the summary")
        if questions:
            reasons.append("open questions for the author")
        set_status(sha, "failure", "Changes requested: " + " and ".join(reasons))


def error():
    with open(META, encoding="utf-8") as f:
        meta = json.load(f)
    set_status(meta["sha"], "error", "AI review failed. A maintainer can comment /ai-review to retry")


if __name__ == "__main__":
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    if command == "prepare":
        prepare()
    elif command == "publish":
        publish(sys.argv[2])
    elif command == "error":
        error()
    else:
        sys.exit("usage: ai_review.py prepare | publish <result.json> | error")
