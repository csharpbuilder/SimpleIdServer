﻿// Copyright (c) SimpleIdServer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
namespace SimpleIdServer.IdServer.Migration;

public class MigrateGroupsCommand
{
    public string Name
    {
        get; set;
    }

    public string Realm
    {
        get; set;
    }

    public int Index
    {
        get; set;
    }

    public int PageSize
    {
        get; set;
    }
}
