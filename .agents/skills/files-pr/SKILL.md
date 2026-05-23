---
name: files-pr
description: Create one or more pull requests.
---

## Title

- Use concise imperative titles that describe the behavior change
- Prefer prepend the PR type:
  - "Fix": use this prefix when the linked issue is a bug
  - "Feature": use this prefix when the linked issue is a feature request
  - "Code Quality": Anything else
- Avoid vague titles like "Update code" or "Refactor files"

## Body

Use the following format.

```
## Resolved / Related Issues

- Closes #ISSUE_NUMBER

## Summary

Write a summary with bullet points if necessary. Focus on the behavior change, not the implementation details unless it's a code quality improvement.

## Validation Steps

Write a list of steps to test this PR.
```
