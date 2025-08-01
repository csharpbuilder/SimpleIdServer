﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using FormBuilder.Helpers;
using FormBuilder.Models;
using FormBuilder.Repositories;
using FormBuilder.Stores;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleIdServer.IdServer.Builders;
using SimpleIdServer.IdServer.Config;
using SimpleIdServer.IdServer.Domains;
using SimpleIdServer.IdServer.Exceptions;
using SimpleIdServer.IdServer.Jwt;
using SimpleIdServer.IdServer.Resources;
using SimpleIdServer.IdServer.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleIdServer.IdServer.Api.Realms;

public class RealmsController : BaseController
{
    private readonly IBusControl _busControl;
    private readonly IRealmRepository _realmRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IScopeRepository _scopeRepository;
    private readonly IFileSerializedKeyStore _fileSerializedKeyStore;
    private readonly IGroupRepository _groupRepository;
    private readonly IAuthenticationContextClassReferenceRepository _authenticationContextClassReferenceRepository;
    private readonly ITransactionBuilder _transactionBuilder;
    private readonly IFormStore _formStore;
    private readonly IWorkflowStore _workflowStore;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ITemplateStore _templateStore;
    private readonly ILogger<RealmsController> _logger;

    public RealmsController(
        IBusControl busControl,
        IRealmRepository realmRepository, 
        IUserRepository userRepository,
        IClientRepository clientRepository,
        IScopeRepository scopeRepository,
        IFileSerializedKeyStore fileSerializedKeyStore,
        IGroupRepository groupRepository,
        IAuthenticationContextClassReferenceRepository authenticationContextClassReferenceRepository,
        ITransactionBuilder transactionBuilder,
        ITokenRepository tokenRepository,
        IJwtBuilder jwtBuilder,
        IFormStore formStore,
        IWorkflowStore workflowStore,
        IDateTimeHelper dateTimeHelper,
        ITemplateStore templateStore,
        ILogger<RealmsController> logger) : base(tokenRepository, jwtBuilder)
    {
        _busControl = busControl;
        _realmRepository = realmRepository;
        _userRepository = userRepository;
        _clientRepository = clientRepository;
        _scopeRepository = scopeRepository;
        _fileSerializedKeyStore = fileSerializedKeyStore;
        _groupRepository = groupRepository;
        _transactionBuilder = transactionBuilder;
        _authenticationContextClassReferenceRepository = authenticationContextClassReferenceRepository;
        _formStore = formStore;
        _workflowStore = workflowStore;
        _dateTimeHelper = dateTimeHelper;
        _templateStore = templateStore;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromRoute] string prefix, CancellationToken cancellationToken)
    {
        prefix = prefix ?? Constants.DefaultRealm;
        try
        {
            await CheckAccessToken(prefix, Config.DefaultScopes.Realms.Name);
            var realms = await _realmRepository.GetAll(cancellationToken);
            return new OkObjectResult(realms);
        }
        catch (OAuthException ex)
        {
            _logger.LogError(ex.ToString());
            return BuildError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromRoute] string prefix, [FromBody] AddRealmRequest request, CancellationToken cancellationToken)
    {
        prefix = prefix ?? Constants.DefaultRealm;
        try
        {
            using (var transaction = _transactionBuilder.Build())
            {
                await CheckAccessToken(prefix, DefaultScopes.Realms.Name);
                var existingRealm = await _realmRepository.Get(request.Name, cancellationToken);
                if (existingRealm != null) throw new OAuthException(System.Net.HttpStatusCode.BadRequest, ErrorCodes.INVALID_REQUEST, string.Format(Global.RealmExists, request.Name));
                var clonedForms = new List<FormRecord>();
                var clonedWorkflows = new List<WorkflowRecord>();
                var realm = new Realm { Name = request.Name, Description = request.Description, CreateDateTime = DateTime.UtcNow, UpdateDateTime = DateTime.UtcNow };
                var administratorRole = RealmRoleBuilder.BuildAdministrativeRole(realm);
                var users = await _userRepository.GetUsersBySubjects(DefaultUsers.AllUserNames, Constants.DefaultRealm, cancellationToken);
                var groups = await _groupRepository.GetAllByStrictFullPath(Constants.DefaultRealm, DefaultGroups.AllFullPath, cancellationToken);
                var clients = await _clientRepository.GetAll(Constants.DefaultRealm, DefaultClients.AllClientIds, cancellationToken);
                var scopes = await _scopeRepository.GetAll(Constants.DefaultRealm, DefaultScopes.DefaultRealmScopeNames, cancellationToken);
                var keys = await _fileSerializedKeyStore.GetAll(Constants.DefaultRealm, cancellationToken);
                var acrs = await _authenticationContextClassReferenceRepository.GetAll(Constants.DefaultRealm, cancellationToken);
                var forms = await _formStore.GetAll(prefix, cancellationToken);
                var templates = await _templateStore.GetAll(Constants.DefaultRealm, cancellationToken);
                var workflows = await _workflowStore.GetAll(prefix, cancellationToken);
                var transformationResult = Transform(workflows, forms, request.Name);

                _realmRepository.Add(realm);
                foreach (var template in templates)
                {
                    var clone = template.Clone() as FormBuilder.Models.Template;
                    template.Id = Guid.NewGuid().ToString();
                    template.Realm = request.Name;
                    foreach (var style in template.Styles)
                    {
                        style.Id = Guid.NewGuid().ToString();
                    }

                    _templateStore.Add(template);
                }

                foreach (var user in users)
                {
                    user.Realms.Add(new RealmUser { RealmsName = request.Name });
                    _userRepository.Update(user);
                }

                foreach (var group in groups)
                {
                    group.Realms.Add(new GroupRealm { RealmsName = request.Name });
                    if (group.FullPath == DefaultGroups.AdministratorGroup.FullPath)
                    {
                        foreach (var scope in administratorRole)
                            group.Roles.Add(scope);
                    }

                    _groupRepository.Update(group);
                }

                foreach (var client in clients)
                {
                    client.Realms.Add(realm);
                    _clientRepository.Update(client);
                }

                foreach (var scope in scopes)
                {
                    scope.Realms.Add(realm);
                    _scopeRepository.Update(scope);
                }

                foreach (var key in keys)
                {
                    key.Realms.Add(realm);
                    _fileSerializedKeyStore.Update(key);
                }

                foreach (var form in transformationResult.Forms)
                {
                    _formStore.Add(form);
                }

                foreach (var workflow in transformationResult.Workflows)
                {
                    _workflowStore.Add(workflow);
                }

                foreach (var acr in acrs)
                {
                    realm.AuthenticationContextClassReferences.Add(new AuthenticationContextClassReference
                    {
                        CreateDateTime = DateTime.UtcNow,
                        DisplayName = acr.DisplayName,
                        Id = Guid.NewGuid().ToString(),
                        Name = acr.Name,
                        UpdateDateTime = DateTime.UtcNow,
                        AuthenticationWorkflow = string.IsNullOrWhiteSpace(acr.AuthenticationWorkflow) ? null : transformationResult.MappingWorkflowOldToNewIds[acr.AuthenticationWorkflow]
                    });
                }

                await _formStore.SaveChanges(cancellationToken);
                await _workflowStore.SaveChanges(cancellationToken);
                await _templateStore.SaveChanges(cancellationToken);
                await transaction.Commit(cancellationToken);
                return new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.Created,
                    Content = JsonSerializer.Serialize(realm).ToString(),
                    ContentType = "application/json"
                };
            }
        }
        catch (OAuthException ex)
        {
            _logger.LogError(ex.ToString());
            return BuildError(ex);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] string prefix, string id, CancellationToken cancellationToken)
    {
        prefix = prefix ?? Constants.DefaultRealm;
        try
        {
            await CheckAccessToken(prefix, Config.DefaultScopes.Realms.Name);
            if (id == Constants.DefaultRealm) throw new OAuthException(System.Net.HttpStatusCode.BadRequest, ErrorCodes.INVALID_REQUEST, Global.CannotRemoveMasterRealm);
            using (var transaction = _transactionBuilder.Build())
            {
                var existingRealm = await _realmRepository.Get(id, cancellationToken);
                if (existingRealm == null) throw new OAuthException(System.Net.HttpStatusCode.NotFound, ErrorCodes.NOT_FOUND, string.Format(Global.UnknownRealm, id));
                var sendEndpoint = await _busControl.GetSendEndpoint(new Uri($"queue:{RemoveRealmCommandConsumer.Queuename}"));
                await sendEndpoint.Send(new RemoveRealmCommand
                {
                    Realm = id
                });
                return new NoContentResult();
            }
        }
        catch (OAuthException ex)
        {
            _logger.LogError(ex.ToString());
            return BuildError(ex);
        }
    }

    private TransformationResult Transform(List<WorkflowRecord> workflows, List<FormRecord> forms, string newRealm)
    {
        var result = new TransformationResult();
        var currentDateTime = _dateTimeHelper.GetCurrent();
        foreach(var workflow in workflows.Select(w => w.Clone() as WorkflowRecord).ToList())
        {
            var newId = Guid.NewGuid().ToString();
            result.MappingWorkflowOldToNewIds.Add(workflow.Id, newId);
            var correlationIds = workflow.Steps.Select(s => s.FormRecordCorrelationId);
            workflow.Id = newId;
            workflow.UpdateDateTime = currentDateTime;
            workflow.Realm = newRealm;
            workflow.Links.ForEach(l => l.Id = Guid.NewGuid().ToString());
            foreach (var step in workflow.Steps)
            {
                var newStepId = Guid.NewGuid().ToString();
                var filteredLinks = workflow.Links.Where(l => l.SourceStepId == step.Id || l.Targets.Any(t => t.TargetStepId == step.Id));
                foreach (var link in filteredLinks)
                {
                    if (link.SourceStepId == step.Id)
                    {
                        link.SourceStepId = newStepId;
                    }
                    else
                    {
                        var targetLink = link.Targets.Single(t => t.TargetStepId == step.Id);
                        targetLink.TargetStepId = newStepId;
                    }
                }

                step.Id = newStepId;
            }

            result.Workflows.Add(workflow);
        }

        var allSteps = result.Workflows.SelectMany(w => w.Steps);
        foreach (var form in forms.Select(w => w.Clone() as FormRecord).ToList())
        {
            form.Id = Guid.NewGuid().ToString();
            form.Status = RecordVersionStatus.Published;
            form.VersionNumber = 0;
            form.Realm = newRealm;
            form.UpdateDateTime = currentDateTime;
            result.Forms.Add(form);
        }


        return result;
    }

    private class TransformationResult
    {
        public List<WorkflowRecord> Workflows { get; set; } = new List<WorkflowRecord>();
        public List<FormRecord> Forms { get; set; } = new List<FormRecord>();
        public Dictionary<string, string> MappingWorkflowOldToNewIds { get; set; } = new Dictionary<string, string>();
    }
}
