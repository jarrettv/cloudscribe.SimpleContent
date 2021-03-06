﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:					Joe Audette
// Created:					2016-08-31
// Last Modified:			2016-09-09
// 


using cloudscribe.SimpleContent.Models;
using cloudscribe.SimpleContent.Storage.EFCore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace cloudscribe.SimpleContent.Storage.EFCore
{
    public interface ISimpleContentModelMapper
    {
        void Map(EntityTypeBuilder<ProjectSettings> entity);

        void Map(EntityTypeBuilder<PostEntity> entity);

        
        void Map(EntityTypeBuilder<PostComment> entity);

        void Map(EntityTypeBuilder<PostCategory> entity);

        void Map(EntityTypeBuilder<PageEntity> entity);

        void Map(EntityTypeBuilder<PageComment> entity);

        void Map(EntityTypeBuilder<PageCategory> entity);



    }
}
