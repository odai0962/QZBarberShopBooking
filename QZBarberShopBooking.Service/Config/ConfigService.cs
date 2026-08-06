using Microsoft.EntityFrameworkCore;
using QZBarberShopBooking.Application.DTO.Config;
using QZBarberShopBooking.Application.DTO.Shared;
using QZBarberShopBooking.Application.Interfaces;
using QZBarberShopBooking.Domain.Entities;
using QZBarberShopBooking.Domain.Enums;
using QZBarberShopBooking.Service.DI.DIType;

namespace QZBarberShopBooking.Service.Config
{
    public class ConfigService : IConfigService, IScopedService
    {
        private static readonly string[] PermissionActions = ["view", "create", "edit", "delete"];

        private readonly IRepository<Page> _pageRepository;

        public ConfigService(IRepository<Page> pageRepository)
        {
            _pageRepository = pageRepository;
        }

        public async Task<ModulesResponseDto> GetModulesAsync(int roleId, Platform platform)
        {
            var pages = await _pageRepository.GetAll()
                .Include(p => p.PagePlatforms)
                .Include(p => p.PagePermissions)
                    .ThenInclude(pp => pp.Permission)
                        .ThenInclude(perm => perm.RolePermissions)
                .Where(p => p.PagePlatforms.Any(pl => pl.Platform == platform))
                .OrderBy(p => p.Id)
                .ToListAsync();

            var modules = new List<ModuleDto>();

            foreach (var page in pages)
            {
                var permissions = BuildPermissionMap(page, roleId, out var isVisible);
                if (!isVisible)
                    continue;

                modules.Add(new ModuleDto
                {
                    Code = page.Code,
                    Name = new LocalizedTextDto { En = page.PageNameEn, Ar = page.PageNameAr },
                    Icon = page.Icon,
                    Url = page.Url,
                    Permissions = permissions
                });
            }

            return new ModulesResponseDto { Modules = modules };
        }

        // A page with no configured PagePermissions is unrestricted — visible to every
        // authenticated role. A page WITH configured permissions is only visible if the caller's
        // role holds at least one of them; otherwise it's dropped entirely rather than returned
        // with every action false, so the menu never shows a dead, always-403 item.
        private static Dictionary<string, bool> BuildPermissionMap(Page page, int roleId, out bool isVisible)
        {
            var permissions = PermissionActions.ToDictionary(a => a, _ => false);

            if (page.PagePermissions.Count == 0)
            {
                foreach (var action in PermissionActions)
                    permissions[action] = true;

                isVisible = true;
                return permissions;
            }

            foreach (var pagePermission in page.PagePermissions)
            {
                var action = pagePermission.Permission.Code.Split('.').Last().ToLowerInvariant();
                if (!permissions.ContainsKey(action))
                    continue;

                if (pagePermission.Permission.RolePermissions.Any(rp => rp.RoleId == roleId))
                    permissions[action] = true;
            }

            isVisible = permissions.Values.Any(v => v);
            return permissions;
        }
    }
}
