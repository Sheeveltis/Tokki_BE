using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.SystemConfigs.Commands.Create;
using Tokki.Application.UseCases.SystemConfigs.Commands.Update;
using Tokki.Application.UseCases.SystemConfigs.DTOs;
using Tokki.Application.UseCases.SystemConfigs.Queries.GetAll;
using Tokki.Domain.Entities;

namespace Tokki.UnitTests.Common.TestData
{
    public static class SystemConfigTestData
    {
        public const string DefaultKey = "CFG_MAX_ITEMS";
        public const string DefaultValue = "10";

        // -------- Commands / Queries --------
        public static CreateSystemConfigCommand BuildCreateCommand(
            string? key = null,
            string? value = DefaultValue,
            string? description = "desc",
            string? dataType = "int")
        {
            return new CreateSystemConfigCommand
            {
                Key = key ?? DefaultKey,
                Value = value,
                Description = description,
                DataType = dataType
            };
        }

        public static UpdateSystemConfigCommand BuildUpdateCommand(
            string? key = null,
            string? value = "20",
            string? description = "updated",
            bool isActive = true)
        {
            return new UpdateSystemConfigCommand
            {
                Key = key ?? DefaultKey,
                Value = value,
                Description = description,
                IsActive = isActive
            };
        }

        public static GetAllSystemConfigsQuery BuildGetAllQuery(int pageNumber = 1, int pageSize = 10)
        {
            return new GetAllSystemConfigsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // -------- Entities --------
        public static SystemConfig BuildEntity(
            string? key = null,
            string? value = DefaultValue,
            string? description = "desc",
            string? dataType = "int",
            bool isActive = true)
        {
            return new SystemConfig
            {
                Key = key ?? DefaultKey,
                Value = value,
                Description = description,
                DataType = dataType,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static List<SystemConfig> BuildEntities(int count)
        {
            var list = new List<SystemConfig>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(BuildEntity(
                    key: $"CFG_{i:00}",
                    value: i.ToString(),
                    description: $"desc {i}",
                    dataType: "int",
                    isActive: i % 2 == 0
                ));
            }
            return list;
        }
    }
}
