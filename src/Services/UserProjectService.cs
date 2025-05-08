using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using taskloom.Models;
using taskloom.Data;
using Microsoft.CodeAnalysis;
using DocumentFormat.OpenXml.Spreadsheet;
using Project = taskloom.Models.Project;
using Microsoft.EntityFrameworkCore;

namespace taskloom.Services
{
    public class UserProjectService
    {
        private readonly taskloomContext _context;

        public UserProjectService(taskloomContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получает текущего пользователя из сессии.
        /// </summary>
        /// <param name="httpContext">HTTP-контекст.</param>
        /// <returns>Текущий пользователь. Null если пользователь не найден.</returns>
        public async Task<User?> GetCurrentUser(HttpContext httpContext)
        {
            int? userSessionID = httpContext.Session.GetInt32("UserID");
            if (userSessionID == null)
                return null;

            User? user = await _context.User.FindAsync(userSessionID);
            return user;
        }
        /// <summary>
        /// Получает пользователя по его идентификатору.
        /// </summary>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <returns>Текущий пользователь. Null если пользователь не найден.</returns>
        public async Task<User?> GetUserByID(int? userID)
        {
            if (userID == null)
                return null;

            User? user = await _context.User.FindAsync(userID);
            return user;
        }
        /// <summary>
        /// Получает проект по его идентификатору, включая связанные данные.
        /// </summary>
        /// <param name="projectID">Идентификатор проекта.</param>
        /// <returns>Проект. Null если проект не найден.</returns>
        public async Task<Project?> GetProjectByID(int? projectID)
        {
            if (projectID == null)
                return null;

            Project? project = await _context.Project
                .Include(x => x.Issues)
                .Include(x=>x.StatusTypes)
                .Include(x=>x.PriorityTypes)
                .Include(x=>x.CategoryTypes)
                .Include(x=>x.UserProjects)
                .ThenInclude(u => u.User)
                .FirstOrDefaultAsync(x=>x.ID==projectID);
            return project;
        }
        /// <summary>
        /// Получает связь пользователя с проектом по идентификаторам проекта и пользователя.
        /// </summary>
        /// <param name="projectID">Идентификатор проекта.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <returns>Связь пользователя с проектом. Null если связь не найдена.</returns>
        public async Task<UserProject?> GetUserProjectByID(int? projectID, int? userID)
        {
            if (projectID == null || userID == null)
                return null;

            UserProject? userProject = await _context.UserProject
                .FirstOrDefaultAsync(x=>x.ProjectID==projectID && 
                    x.UserID==userID);

            return userProject;
        }

        /// <summary>
        /// Получает связь пользователя с проектом по идентификатору связи.
        /// </summary>
        /// <param name="userProjectID">Идентификатор связи пользователя с проектом.</param>
        /// <returns>Связь пользователя с проектом. Null если связь не найдена.</returns>
        public async Task<UserProject?> GetUserProjectByID(int? userProjectID)
        {
            if (userProjectID == null)
                return null;

            UserProject? userProject = await _context.UserProject.FindAsync(userProjectID);

            return userProject;
        }

        /// <summary>
        /// Проверяет, состоит ли пользователь в проекте.
        /// </summary>
        /// <param name="projectID">Идентификатор проекта.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <returns>Значение true, если пользователь состоит в проекте, иначе false.</returns>
        public async Task<bool> UserIsInProject(int? projectID, int? userID)
        {
            if (projectID == null || userID == null)
                return false;

            UserProject? userProject = await _context.UserProject
                .FirstOrDefaultAsync(x => x.ProjectID == projectID &&
                    x.UserID == userID);
            if (userProject == null) 
                return false;

            return true;
        }

    }
}
