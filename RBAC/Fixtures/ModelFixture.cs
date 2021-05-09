using System;
using System.IO;
using System.Text;
using NetCasbin.Persist.FileAdapter;

namespace RBAC.Fixtures
{
    public class ModelFixture
    {
        internal readonly string _rbacModelText = ReadFile("rbac_model.conf");
        internal readonly string _rbacPolicyText = ReadFile("rbac_policy.csv");

        internal readonly string _abacModelText = ReadFile("abac_model.conf");
        internal readonly string _abacWithEvalModelText = ReadFile("abac_rule_model.conf");
        internal readonly string _abacWithEvalPolicyText = ReadFile("abac_rule_policy.csv");
        internal readonly string _basicModelText = ReadFile("basic_model.conf");
        internal readonly string _basicPolicyText = ReadFile("basic_policy.csv");
        internal readonly string _basicWithoutResourceModelText = ReadFile("basic_without_resources_model.conf");
        internal readonly string _basicWithoutResourcePolicyText = ReadFile("basic_without_resources_policy.csv");
        internal readonly string _basicWithoutUserModelText = ReadFile("basic_without_users_model.conf");
        internal readonly string _basicWithoutUserPolicyText = ReadFile("basic_without_users_policy.csv");
        internal readonly string _keyMatchModelText = ReadFile("keymatch_model.conf");
        internal readonly string _keyMatchPolicyText = ReadFile("keymatch_policy.csv");
        internal readonly string _keyMatch2ModelText = ReadFile("keymatch2_model.conf");
        internal readonly string _keyMatch2PolicyText = ReadFile("keymatch2_policy.csv");
        internal readonly string _priorityModelText = ReadFile("priority_model.conf");
        internal readonly string _priorityPolicyText = ReadFile("priority_policy.csv");

        public ModelFixture()
        {

        }

        public NetCasbin.Model.Model GetNewAbacModel()
        {
            return GetNewModel(_abacModelText);
        }

        public NetCasbin.Model.Model GetNewAbacWithEvalModel()
        {
            return GetNewModel(_abacWithEvalModelText, _abacWithEvalPolicyText);
        }

        public NetCasbin.Model.Model GetBasicTestModel()
        {
            return GetNewModel(_basicModelText, _basicPolicyText);
        }

        public NetCasbin.Model.Model GetBasicWithoutResourceModel()
        {
            return GetNewModel(_basicWithoutResourceModelText, _basicWithoutResourcePolicyText);
        }

        public NetCasbin.Model.Model GetBasicWithoutUserModel()
        {
            return GetNewModel(_basicWithoutUserModelText, _basicWithoutUserPolicyText);
        }

        public NetCasbin.Model.Model GetNewKeyMatchModel()
        {
            return GetNewModel(_keyMatchModelText, _keyMatchPolicyText);
        }

        public NetCasbin.Model.Model GetNewKeyMatch2Model()
        {
            return GetNewModel(_keyMatch2ModelText, _keyMatch2PolicyText);
        }

        public NetCasbin.Model.Model GetNewPriorityModel()
        {
            return GetNewModel(_priorityModelText, _priorityPolicyText);
        }

        public NetCasbin.Model.Model GetNewRbacModel()
        {
            return GetNewModel(_rbacModelText, _rbacPolicyText);
        }

        public static NetCasbin.Model.Model GetNewModel(string modelText)
        {
            return NetCasbin.Model.Model.CreateDefaultFromText(modelText);
        }

        public static NetCasbin.Model.Model GetNewModel(string modelText, string policyText)
        {
            return LoadModelFromMemory(GetNewModel(modelText), policyText);
        }

        public static string GetFile(string fileName)
        {
            return Path.Combine("data", fileName);
        }

        private static NetCasbin.Model.Model LoadModelFromMemory(NetCasbin.Model.Model model, string policy)
        {
            model.ClearPolicy();
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(policy)))
            {
                DefaultFileAdapter fileAdapter = new DefaultFileAdapter(ms);
                fileAdapter.LoadPolicy(model);
            }
            model.RefreshPolicyStringSet();
            return model;
        }

        private static string ReadFile(string fileName)
        {
            return File.ReadAllText(GetFile(fileName));
        }
    }
}
