﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using FormBuilder;
using FormBuilder.Link;
using FormBuilder.Models.Layout;
using FormBuilder.Models.Rules;
using FormBuilder.Models.Transformer;
using FormBuilder.Transformers;
using SimpleIdServer.IdServer.Layout;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace SimpleIdServer.IdServer.Pwd;

public class ResetPwdWorkflowLayout : IWorkflowLayoutService
{
    public string Category => FormCategories.Authentication;

    public WorkflowLayout Get()
    {
        return new WorkflowLayout
        {
            Name = "resetPwd",
            WorkflowCorrelationId = "resetPwd",
            SourceFormCorrelationId = "resetPwd",
            Links = new List<WorkflowLinkLayout>
            {
                // Reset
                new WorkflowLinkLayout
                {
                    IsMainLink = true,
                    EltCorrelationId = StandardPwdAuthForms.pwdResetFormId,
                    Targets = new List<WorkflowLinkTargetLayout>
                    {
                        new WorkflowLinkTargetLayout 
                        { 
                            TargetFormCorrelationId = StandardPwdAuthForms.ConfirmResetForm.CorrelationId,
                            Description = "Reset"
                        }
                    },
                    ActionType = WorkflowLinkHttpRequestAction.ActionType,
                    ActionParameter = JsonSerializer.Serialize(new WorkflowLinkHttpRequestParameter
                    {
                        Method = HttpMethods.POST,
                        IsAntiforgeryEnabled = true,
                        Target = "/{realm}/pwd/Reset",
                        Transformers = new List<ITransformerParameters>
                            {
                                new RegexTransformerParameters()
                                {
                                    Rules = new ObservableCollection<MappingRule>
                                    {
                                        new MappingRule { Source = "$.Realm", Target = "realm" }
                                    }
                                },
                                new RelativeUrlTransformerParameters()
                            }
                    })
                }
            }
        };
    }
}