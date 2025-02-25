﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace SimpleIdServer.IdServer.UI.ViewModels;

public interface IRegisterViewModel : ISidStepViewModel
{
    bool IsUpdated { get; set; }
    bool IsCreated { get; set; }
    bool UpdateOneCredential { get; set; }
}