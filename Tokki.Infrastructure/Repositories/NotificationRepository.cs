using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
 
namespace Tokki.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly TokkiDbContext _context;
 
        public NotificationRepository(TokkiDbContext context)
        {
            _context = context;
        }
 
        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
            
            var account = await _context.Accounts.FindAsync(notification.UserId);
            if (account != null)
            {
                account.UnreadNotificationCount++;
            }
        }
 
        public async Task<Notification?> GetByIdAsync(string id)
        {
            return await _context.Notifications.FindAsync(id);
        }
 
        public async Task<IEnumerable<Notification>> GetPagedByUserIdAsync(string userId, int pageNumber, int pageSize, bool? isRead)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);
            
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
 
        public async Task<int> CountUnreadAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<int> CountTotalByUserIdAsync(string userId, bool? isRead)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);
            
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }

            return await query.CountAsync();
        }
 
        public async Task MarkAsReadAsync(string id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                
                var account = await _context.Accounts.FindAsync(notification.UserId);
                if (account != null && account.UnreadNotificationCount > 0)
                {
                    account.UnreadNotificationCount--;
                }
            }
        }
 
        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
                
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
 
            var account = await _context.Accounts.FindAsync(userId);
            if (account != null)
            {
                account.UnreadNotificationCount = 0;
            }
        }
 
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
