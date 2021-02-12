/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core;
using BaseEAM.Core.Domain;
using BaseEAM.Services;
using BaseEAM.Workflows.Core.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BaseEAM.Workflows.Activities
{
    public class ActivityHelper
    {
        /// <summary>
        /// Get users by reflecting on a method signature
        /// </summary>
        /// <param name="methodSignature">Ex: IWorkOrderService.GetCreatedUser</param>
        /// <param name="entityId"></param>
        /// <returns>A string of users seperated by ;</returns>
        public static string GetUsers(string methodSignature, long entityId)
        {
            string result = "";
            if (methodSignature.Contains(".") && methodSignature.Contains("Service"))
            {
                var array = methodSignature.Split('.');
                if (array.Length != 2)
                    throw new BaseEamException("Wrong method call syntax in workflow");
                var serviceInterface = array[0];
                var methodName = array[1];

                Assembly asm = typeof(WorkflowBaseService).Assembly;
                Type type = asm.GetType("BaseEAM.Services." + serviceInterface);
                var service = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve(type);
                MethodInfo method = type.GetMethod(methodName);
                List<User> users = method.Invoke(service, new object[] { entityId }) as List<User>;
                result = string.Join(";", users.Select(u => u.Email));
            }

            return result;
        }

        /// <summary>
        /// Execute an action
        /// The action is based on a method signature
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="entityId"></param>
        public static void ExecuteAction(string actionName, long entityId)
        {
            if (actionName.Contains(".") && actionName.Contains("Service"))
            {
                var array = actionName.Split('.');
                if (array.Length != 2)
                    throw new BaseEamException("Wrong method call syntax in workflow");
                var serviceInterface = array[0];
                var methodName = array[1];

                Assembly asm = typeof(WorkflowBaseService).Assembly;
                Type type = asm.GetType("BaseEAM.Services." + serviceInterface);
                var service = WorkflowServiceEngine.Instance.WorkflowContainerManager.Resolve(type);
                MethodInfo method = type.GetMethod(methodName);
                method.Invoke(service, new object[] { entityId });
            }
        }
    }
}
