using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NetCasbin;
using NetCasbin.Rbac;
using NetCasbin.Util;

namespace RBAC.Model
{
    /// <summary>
    /// Represents an expression in a section of the model.
    /// For example: r = sub, obj, act
    /// </summary>
    public class Assertion
    {
        public string Key { set; get; }

        public string Value { set; get; }

        public IDictionary<string, int> Tokens { set; get; }

        public IRoleManager RoleManager { get; internal set; }

        public List<List<string>> Policy { set; get; }

        private HashSet<string> PolicyStringSet { get; }

        public Assertion()
        {
            Policy = new List<List<string>>();
            PolicyStringSet = new HashSet<string>();
            RoleManager = new DefaultRoleManager(10);
        }

        public void RefreshPolicyStringSet()
        {
            PolicyStringSet.Clear();
            foreach (var rule in Policy)
            {
                PolicyStringSet.Add(Utility.RuleToString(rule));
            }
        }

        internal void BuildIncrementalRoleLink(PolicyOperation policyOperation, IEnumerable<string> rule)
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            BuildRoleLink(count, policyOperation, rule);
        }

        internal void BuildIncrementalRoleLinks(PolicyOperation policyOperation, IEnumerable<IEnumerable<string>> rules)
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            foreach (var rule in rules)
            {
                BuildRoleLink(count, policyOperation, rule);
            }
        }

        public void BuildRoleLinks()
        {
            int count = Value.Count(c => c is '_');
            if (count < 2)
            {
                throw new InvalidOperationException("the number of \"_\" in role definition should be at least 2.");
            }

            foreach (var rule in Policy)
            {
                BuildRoleLink(count, PolicyOperation.PolicyAdd, rule);
            }
        }

        private void BuildRoleLink(int groupPolicyCount,
            PolicyOperation policyOperation, IEnumerable<string> rule)
        {
            var roleManager = RoleManager;
            List<string> ruleEnum = rule as List<string> ?? rule.ToList();
            int ruleCount = ruleEnum.Count;

            if (ruleCount < groupPolicyCount)
            {
                throw new InvalidOperationException("Grouping policy elements do not meet role definition.");
            }

            if (ruleCount > groupPolicyCount)
            {
                ruleEnum = ruleEnum.GetRange(0, groupPolicyCount);
            }

            switch (policyOperation)
            {
                case PolicyOperation.PolicyAdd:
                    switch (groupPolicyCount)
                    {
                        case 2:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1]);
                            break;
                        case 3:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1], ruleEnum[2]);
                            break;
                        case 4:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1],
                                ruleEnum[2], ruleEnum[3]);
                            break;
                        default:
                            roleManager.AddLink(ruleEnum[0], ruleEnum[1],
                                ruleEnum.GetRange(2, groupPolicyCount - 2).ToArray());
                            break;
                    }
                    break;
                case PolicyOperation.PolicyRemove:
                    switch (groupPolicyCount)
                    {
                        case 2:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1]);
                            break;
                        case 3:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1], ruleEnum[2]);
                            break;
                        case 4:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1],
                                ruleEnum[2], ruleEnum[3]);
                            break;
                        default:
                            roleManager.DeleteLink(ruleEnum[0], ruleEnum[1],
                                ruleEnum.GetRange(2, groupPolicyCount - 2).ToArray());
                            break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(policyOperation), policyOperation, null);
            }

        }

        internal bool Contains(IEnumerable<string> rule)
        {
            return PolicyStringSet.Contains(Utility.RuleToString(rule));
        }

        internal bool TryAddPolicy(List<string> rule)
        {
            if (Contains(rule))
            {
                return false;
            }
            Policy.Add(rule);
            PolicyStringSet.Add(Utility.RuleToString(rule));
            return true;
        }

        internal bool TryRemovePolicy(List<string> rule)
        {
            if (!Contains(rule))
            {
                return false;
            }
            for (int i = 0; i < Policy.Count; i++)
            {
                var ruleInPolicy = Policy[i];
                if (!Utility.ArrayEquals(rule, ruleInPolicy))
                {
                    continue;
                }
                Policy.RemoveAt(i);
                PolicyStringSet.Remove(Utility.RuleToString(rule));
                break;
            }
            return true;
        }

        internal void ClearPolicy()
        {
            Policy.Clear();
            PolicyStringSet.Clear();
        }
    }
}
