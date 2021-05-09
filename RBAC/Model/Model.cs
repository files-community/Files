using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetCasbin.Util;

namespace RBAC.Model
{
    public class Model : Policy
    {
        private static readonly IDictionary<string, string> _sectionNameMap = new Dictionary<string, string>() {
            { PermConstants.Section.RequestSection, PermConstants.Section.RequestSectionName},
            { PermConstants.Section.PolicySection, PermConstants.Section.PolicySectionName},
            { PermConstants.Section.RoleSection, PermConstants.Section.RoleSectionName},
            { PermConstants.Section.PolicyEffectSection, PermConstants.Section.PolicyEffectSectionName},
            { PermConstants.Section.MatcherSection, PermConstants.Section.MatcherSectionName}
        };

        /// <summary>
        /// Creates a model.
        /// </summary>
        /// <returns></returns>
        public static Model CreateDefault()
        {
            return new Model();
        }

        /// <summary>
        /// Creates a model.
        /// </summary>
        /// <param name="path">The path of the model file.</param>
        /// <returns></returns>
        public static Model CreateDefaultFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Can not find the model file.");
            }

            var model = CreateDefault();
            model.LoadModel(path);
            return model;
        }

        /// <summary>
        /// Creates a model.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Model CreateDefaultFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            var model = CreateDefault();
            model.LoadModelFromText(text);
            return model;
        }

        public void LoadModel(string path)
        {
            LoadModel(Config.Config.NewConfig(path));
        }

        public void LoadModelFromText(string text)
        {
            LoadModel(Config.Config.NewConfigFromText(text));
        }

        /// <summary>
        /// Adds an assertion to the model.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key">Key of the assertion, it is same to "policyType" when section is "p" or "g".</param>
        /// <param name="value">Value of the assertion.</param>
        /// <returns></returns>
        public bool AddDef(string section, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var assertion = new Assertion
            {
                Key = key,
                Value = value
            };

            if (section.Equals(PermConstants.Section.RequestSection) ||
                section.Equals(PermConstants.Section.PolicySection))
            {
                string[] tokens = assertion.Value
                    .Split(PermConstants.PolicySeparatorChar)
                    .Select(t => t.Trim()).ToArray();

                if (tokens.Length != 0)
                {
                    assertion.Tokens = new Dictionary<string, int>();
                    for (int i = 0; i < tokens.Length; i++)
                    {
                        assertion.Tokens.Add($"{key}_{tokens[i]}", i);
                    }
                }
            }
            else
            {
                assertion.Value = Utility.RemoveComments(Utility.EscapeAssertion(assertion.Value));
            }

            if (Model.ContainsKey(section) is false)
            {
                var assertionMap = new Dictionary<string, Assertion>
                {
                    [key] = assertion
                };
                Model.Add(section, assertionMap);
            }
            else
            {
                Model[section].Add(key, assertion);
            }

            return true;
        }

        private void LoadModel(Config.Config config)
        {
            LoadSection(config, PermConstants.Section.RequestSection);
            LoadSection(config, PermConstants.Section.PolicySection);
            LoadSection(config, PermConstants.Section.RoleSection);
            LoadSection(config, PermConstants.Section.PolicyEffectSection);
            LoadSection(config, PermConstants.Section.MatcherSection);
        }

        private void LoadSection(Config.Config config, string section)
        {
            int i = 1;
            while (true)
            {
                string key = string.Concat(section, GetKeySuffix(i));
                if (!LoadAssertion(config, section, key))
                {
                    break;
                }
                i++;
            }
        }

        private bool LoadAssertion(Config.Config config, string section, string key)
        {
            string sectionName = _sectionNameMap[section];
            string value = config.GetString($"{sectionName}::{key}");
            return AddDef(section, key, value);
        }

        private static string GetKeySuffix(int i)
        {
            return i == 1 ? string.Empty : i.ToString();
        }
    }
}
