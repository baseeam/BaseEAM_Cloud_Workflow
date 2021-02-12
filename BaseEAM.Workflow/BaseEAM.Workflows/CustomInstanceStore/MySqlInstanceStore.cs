using BaseEAM.Core.Domain;
using BaseEAM.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace BaseEAM.Workflows.CustomInstanceStore
{
    /// <summary>
    /// Persists instance data to the MySql database
    /// </summary>
    public class MySqlInstanceStore : CustomInstanceStoreBase
    {
        public MySqlInstanceStore(Guid storeId) : base(storeId) { }

        public override void Save(Guid instanceId, Guid storeId, XmlDocument doc)
        {
            var repo = GetWorkflowInstanceRepo();
            var instance = GetWorkflowInstance(instanceId);
            if(instance == null)
            {
                instance = new WorkflowInstance
                {
                    InstanceId = instanceId.ToString(),
                    InstanceData = doc.OuterXml
                };
                repo.Insert(instance);
            }
            else
            {
                instance.InstanceData = doc.OuterXml;
                repo.Update(instance);
            }
        }
        public override XmlDocument Load(Guid instanceId, Guid storeId)
        {
            var repo = GetWorkflowInstanceRepo();
            var instance = GetWorkflowInstance(instanceId);
            if (instance == null)
            {
                throw new Exception("instance not found.");
            }
            XmlDocument instanceData = new XmlDocument();
            instanceData.LoadXml(instance.InstanceData);
            return instanceData;
        }
        private WorkflowInstance GetWorkflowInstance(Guid instanceId)
        {
            var repo = GetWorkflowInstanceRepo();
            var sql = string.Format("SELECT * FROM WorkflowInstance WHERE InstanceId = '{0}'", instanceId.ToString());
            IEnumerable<WorkflowInstance> instances = repo.Query(sql);
            if(instances.Count() > 0)
            {
                return instances.ElementAt(0);
            }
            else
            {
                return null;
            }
        }
        private DapperRepository<WorkflowInstance> GetWorkflowInstanceRepo()
        {
            string dataConnectionString = ConfigurationManager.ConnectionStrings["BaseEAM"].ConnectionString;
            var dapperContext = new DapperContext(dataConnectionString);
            var repo = new DapperRepository<WorkflowInstance>(dapperContext);
            return repo;
        }

    }
}
