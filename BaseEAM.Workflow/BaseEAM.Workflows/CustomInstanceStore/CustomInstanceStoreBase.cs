﻿using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.IO;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace BaseEAM.Workflows.CustomInstanceStore
{
    public abstract class CustomInstanceStoreBase : InstanceStore, IDisposable
    {
        /// <summary>
        /// A unique identifier for the store of instances. There will usually be one store id for all workflows
        /// in an application. If one is not specified, then one will be generated.
        /// </summary>
        private Guid _storeId;

        /// <summary>
        /// Internal handle used to identify the workflow owner.
        /// </summary>
        //private InstanceHandle _handle;

        public CustomInstanceStoreBase(Guid storeId)
        {
            _storeId = storeId;

            //_handle = this.CreateInstanceHandle();
            //var view = this.Execute(_handle, new CreateWorkflowOwnerCommand(), TimeSpan.FromSeconds(30));
            //this.DefaultInstanceOwner = view.InstanceOwner;
        }

        public abstract void Save(Guid instanceId, Guid storeId, XmlDocument doc);
        public abstract XmlDocument Load(Guid instanceId, Guid storeId);

        // Synchronous version of the Begin/EndTryCommand functions
        protected override bool TryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout)
        {
            return EndTryCommand(BeginTryCommand(context, command, timeout, null, null));
        }

        // The persistence engine will send a variety of commands to the configured InstanceStore,
        // such as CreateWorkflowOwnerCommand, SaveWorkflowCommand, and LoadWorkflowCommand.
        // This method is where we will handle those commands.
        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            IDictionary<XName, InstanceValue> instanceStateData = null;

            //The CreateWorkflowOwner command instructs the instance store to create a new instance owner bound to the instanace handle
            if (command is CreateWorkflowOwnerCommand)
            {
                context.BindInstanceOwner(_storeId, Guid.NewGuid());
            }
            //The SaveWorkflow command instructs the instance store to modify the instance bound to the instance handle or an instance key
            else if (command is SaveWorkflowCommand)
            {
                SaveWorkflowCommand saveCommand = (SaveWorkflowCommand)command;
                instanceStateData = saveCommand.InstanceData;

                var instanceStateXml = DictionaryToXml(instanceStateData);
                Save(context.InstanceView.InstanceId, this._storeId, instanceStateXml);
            }
            //The LoadWorkflow command instructs the instance store to lock and load the instance bound to the identifier in the instance handle
            else if (command is LoadWorkflowCommand)
            {
                var xml = Load(context.InstanceView.InstanceId, this._storeId);
                instanceStateData = XmlToDictionary(xml);
                //load the data into the persistence Context
                context.LoadedInstance(InstanceState.Initialized, instanceStateData, null, null, null);
            }

            return new CompletedAsyncResult<bool>(true, callback, state);
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            return CompletedAsyncResult<bool>.End(result);
        }

        // Converts XML data back to the original form.
        private IDictionary<XName, InstanceValue> XmlToDictionary(XmlDocument doc)
        {
            IDictionary<System.Xml.Linq.XName, InstanceValue> data = new Dictionary<System.Xml.Linq.XName, InstanceValue>();

            NetDataContractSerializer s = new NetDataContractSerializer();

            XmlNodeList instances = doc.GetElementsByTagName("InstanceValue");
            foreach (XmlElement instanceElement in instances)
            {
                XmlElement keyElement = (XmlElement)instanceElement.SelectSingleNode("descendant::key");
                System.Xml.Linq.XName key = (System.Xml.Linq.XName)DeserializeObject(s, keyElement);

                XmlElement valueElement = (XmlElement)instanceElement.SelectSingleNode("descendant::value");
                object value = DeserializeObject(s, valueElement);
                InstanceValue instVal = new InstanceValue(value);

                data.Add(key, instVal);
            }

            return data;
        }
        object DeserializeObject(NetDataContractSerializer serializer, XmlElement element)
        {
            object deserializedObject = null;

            MemoryStream stm = new MemoryStream();
            XmlDictionaryWriter wtr = XmlDictionaryWriter.CreateTextWriter(stm);
            element.WriteContentTo(wtr);
            wtr.Flush();
            stm.Position = 0;

            deserializedObject = serializer.Deserialize(stm);

            return deserializedObject;
        }

        // Converts the persistence data to XML form.
        XmlDocument DictionaryToXml(IDictionary<XName, InstanceValue> instanceData)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<InstanceValues/>");

            foreach (KeyValuePair<XName, InstanceValue> valPair in instanceData)
            {
                XmlElement newInstance = doc.CreateElement("InstanceValue");

                XmlElement newKey = SerializeObject("key", valPair.Key, doc);
                newInstance.AppendChild(newKey);

                XmlElement newValue = SerializeObject("value", valPair.Value.Value, doc);
                newInstance.AppendChild(newValue);

                doc.DocumentElement.AppendChild(newInstance);
            }

            return doc;
        }
        XmlElement SerializeObject(string elementName, object o, XmlDocument doc)
        {
            NetDataContractSerializer s = new NetDataContractSerializer();
            XmlElement newElement = doc.CreateElement(elementName);
            MemoryStream stm = new MemoryStream();

            s.Serialize(stm, o);
            stm.Position = 0;
            StreamReader rdr = new StreamReader(stm);
            newElement.InnerXml = rdr.ReadToEnd();

            return newElement;
        }

        public void Dispose()
        {
            //this.Execute(_handle, new DeleteWorkflowOwnerCommand(), TimeSpan.FromSeconds(30));
            //_handle.Free();
        }
    }
}
