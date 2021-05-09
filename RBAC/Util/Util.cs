using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using NetCasbin;
using NetCasbin.Rbac;
using NetCasbin.Util;

namespace RBAC.Util
{
    public static class Util
    {
        internal static List<T> AsList<T>(params T[] values)
        {
            return values.ToList();
        }

        internal static List<string> AsList(params string[] values)
        {
            return values.ToList();
        }

        internal static bool Enforce(Enforcer e, object sub, object obj, string act)
        {
            return e.Enforce(sub, obj, act);
        }

#if !NET452
        internal static List<string> EnforceEx(Enforcer e, object sub, object obj, string act)
        {
            var myRes = e.EnforceEx(sub, obj, act).Explains.ToList();
            if (myRes.Count > 0)
            {
                return  myRes[0].ToList();
            }

            return null;
        }
#else
        internal static void TestEnforceEx(Enforcer e, object sub, object obj, string act, List<string> res)
        {
            var myRes = e.EnforceEx(sub, obj, act).Item2.ToList();
            string message = "Key: " + myRes + ", supposed to be " + res;
            if (myRes.Count > 0)
            {
                Assert.True(Utility.SetEquals(res, myRes[0].ToList()), message);
            }
        }
#endif

        internal static async Task<List<string>> EnforceExAsync(Enforcer e, object sub, object obj, string act)
        {
            var myRes = (await e.EnforceExAsync(sub, obj, act)).Item2.ToList();
            if (myRes.Count > 0)
            {
                return myRes[0].ToList();
            }

            return null;
        }

        internal static async Task<bool> EnforceAsync(Enforcer e, object sub, object obj, string act)
        {
            return  await e.EnforceAsync(sub, obj, act);
        }

        internal static bool EnforceWithoutUsers(Enforcer e, string obj, string act)
        {
            return  e.Enforce(obj, act);
        }

        internal static async Task<bool> EnforceWithoutUsersAsync(Enforcer e, string obj, string act)
        {
            return  await e.EnforceAsync(obj, act);
        }

        internal static bool DomainEnforce(Enforcer e, string sub, string dom, string obj, string act)
        {
            return e.Enforce(sub, dom, obj, act);
        }

        internal static List<List<string>> GetPolicy(Enforcer e)
        {
            try
            {
                List<List<string>> myRes = e.GetPolicy();
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<List<string>> GetFilteredPolicy(Enforcer e, int fieldIndex, params string[] fieldValues)
        {
            try
            {
                List<List<string>> myRes = e.GetFilteredPolicy(fieldIndex, fieldValues);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<List<string>> GetGroupingPolicy(Enforcer e)
        {
            try
            {
                List<List<string>> myRes = e.GetGroupingPolicy();
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<List<string>> GetFilteredGroupingPolicy(Enforcer e, int fieldIndex, params string[] fieldValues)
        {
            try
            {
                List<List<string>> myRes = e.GetFilteredGroupingPolicy(fieldIndex, fieldValues);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasPolicy(Enforcer e, List<string> policy)
        {
            return e.HasPolicy(policy);
        }

        internal static bool HasGroupingPolicy(Enforcer e, List<string> policy)
        {
            return e.HasGroupingPolicy(policy);
        }

        internal static List<string> GetRoles(Enforcer e, string name, string domain = null)
        {
            try
            {
                List<string> myRes = e.GetRolesForUser(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<string> GetUsers(Enforcer e, string name, string domain = null)
        {
            try
            {
                List<string> myRes = e.GetUsersForRole(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasRole(Enforcer e, string name, string role, string domain = null)
        {
            return e.HasRoleForUser(name, role, domain);
        }

        internal static List<List<string>> GetPermissions(Enforcer e, string name, string domain = null)
        {
            try
            {
                List<List<string>> myRes = e.GetPermissionsForUser(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<List<string>> GetImplicitPermissions(Enforcer e, string name, string domain = null)
        {
            try
            {
                List<List<string>> myRes = e.GetImplicitPermissionsForUser(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasPermission(Enforcer e, string name, List<string> permission)
        {
            return e.HasPermissionForUser(name, permission);
        }

        internal static List<string> GetRolesInDomain(Enforcer e, string name, string domain)
        {
            try
            {
                List<string> myRes = e.GetRolesForUserInDomain(name, domain);
                return myRes;
            }
            catch 
            {
                return null;
            }
        }

        internal static List<string> GetImplicitRolesInDomain(Enforcer e, string name, string domain)
        {
            try
            {
                List<string> myRes = e.GetImplicitRolesForUser(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        internal static List<List<string>> GetPermissionsInDomain(Enforcer e, string name, string domain, List<List<string>> res)
        {
            try
            {
                List<List<string>> myRes = e.GetPermissionsForUserInDomain(name, domain);
                return myRes;
            }
            catch
            {
                return null;
            }
        }

        #region RoleManger test

        internal static bool Role(IRoleManager roleManager, string name1, string name2)
        {
            return roleManager.HasLink(name1, name2);
        }

        internal static bool DomainRole(IRoleManager roleManager, string name1, string name2, string domain)
        {
            return roleManager.HasLink(name1, name2, domain);
        }

        internal static List<string> GetRoles(IRoleManager roleManager, string name, List<string> expectResult)
        {
            try
            {
                List<string> result = roleManager.GetRoles(name);
                return result;
            }
            catch
            {
                return null;
            }
        }

        internal static List<string> GetRolesWithDomain(IRoleManager roleManager, string name, string domain)
        {
            try
            {
                List<string> result = roleManager.GetRoles(name, domain);
                return result;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
