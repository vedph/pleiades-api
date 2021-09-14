using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using PleiadesApi.Models;
using System;
using System.Collections.Generic;

namespace PleiadesApi.Services
{
    /// <summary>
    /// Application registered user mapper.
    /// </summary>
    public class ApplicationRegUserMapper : IUserMapper<ApplicationUser,
        NamedRegisterBindingModel, NamedUserModel>
    {
        /// <summary>
        /// Gets the user model from the specified input model.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <returns>The user model.</returns>
        /// <exception cref="ArgumentNullException">model</exception>
        public ApplicationUser GetModel(NamedRegisterBindingModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            return new ApplicationUser
            {
                UserName = model.Name,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };
        }

        /// <summary>
        /// Gets the output model from the specified user.
        /// </summary>
        /// <param name="user">The object with user data. This is assumed to
        /// be of type <see cref="UserWithRoles{TUser}" />.
        /// repository.</param>
        /// <returns>
        /// The output model.
        /// </returns>
        /// <exception cref="ArgumentNullException">user</exception>
        /// <exception cref="ArgumentException">user</exception>
        public NamedUserModel GetView(object user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            UserWithRoles<ApplicationUser> ur = user as UserWithRoles<ApplicationUser>;
            if (ur == null) throw new ArgumentException(nameof(user));

            return new NamedUserModel
            {
                UserName = ur.User.UserName,
                Email = ur.User.Email,
                FirstName = ur.User.FirstName,
                LastName = ur.User.LastName,
                Roles = ur.Roles,
                EmailConfirmed = ur.User.EmailConfirmed,
                LockoutEnabled = ur.User.LockoutEnabled,
                LockoutEnd = ur.User.LockoutEnd?.UtcDateTime
            };
        }

        /// <summary>
        /// Gets the dictionary used for building messages based on user's data,
        /// like user name, first name, last name, etc.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Dictionary.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public Dictionary<string, string> GetMessageDictionary(ApplicationUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            return new Dictionary<string, string>
            {
                ["FirstName"] = user.FirstName,
                ["LastName"] = user.LastName,
                ["UserName"] = user.UserName
            };
        }
    }
}
