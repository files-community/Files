using NetCasbin;
using RBAC.Fixtures;
using System.Collections.Generic;

namespace RBAC
{
    public class RbacApi
    {
        private ModelFixture _modelFixture;

        public RbacApi(ModelFixture modelFixture)
        {
            ModelFixture = modelFixture;
        }

        public ModelFixture ModelFixture { get => _modelFixture; set => _modelFixture = value; }
    }
}

