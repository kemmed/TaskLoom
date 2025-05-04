using System.Net.Mail;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using diplom.Models;
using diplom.Data;
using Microsoft.CodeAnalysis;
using DocumentFormat.OpenXml.Spreadsheet;
using Project = diplom.Models.Project;
using Microsoft.EntityFrameworkCore;

namespace diplom.Services
{
    public class UserService
    {
        private readonly diplomContext _context;

        public UserService(diplomContext context)
        {
            this._context = context;
        }

        public async Task<User?> GetCurrentUser(HttpContext httpContext)
        {
            int? userSessionID = httpContext.Session.GetInt32("UserID");
            if (userSessionID == null)
                return null;

            User? user = await _context.User.FindAsync(userSessionID);
            return user;
        }
        public async Task<User?> GetUserByID(int? userID)
        {
            if (userID == null)
                return null;

            User? user = await _context.User.FindAsync(userID);
            return user;
        }
        public async Task<Project?> GetProjectByID(int? projectID)
        {
            if (projectID == null)
                return null;

            Project? project = await _context.Project.FindAsync(projectID);
            return project;
        }
        public async Task<UserProject?> GetUserProjectByID(int? projectID, int? userID)
        {
            if (projectID == null || userID == null)
                return null;

            UserProject? userProject = await _context.UserProject
                .FirstOrDefaultAsync(x=>x.ProjectID==projectID && 
                    x.UserID==userID);

            return userProject;
        }
        public async Task<UserProject?> GetUserProjectByID(int? userProjectID)
        {
            if (userProjectID == null)
                return null;

            UserProject? userProject = await _context.UserProject.FindAsync(userProjectID);

            return userProject;
        }

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
