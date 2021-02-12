/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core;
using BaseEAM.Core.Domain;

namespace BaseEAM.WorkflowService
{
    public class WorkflowServiceWorkContext : IWorkContext
    {
        public User CurrentUser { get; set; }

        public Currency WorkingCurrency { get; set; }

        public Language WorkingLanguage { get; set; }
    }
}
