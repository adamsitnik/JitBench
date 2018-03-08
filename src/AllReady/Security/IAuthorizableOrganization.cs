﻿using System.Threading.Tasks;

namespace AllReady.Security
{
    public interface IAuthorizableOrganization : IAuthorizable
    {
        /// <summary>
        /// The ID of the organization that the organization belongs to
        /// </summary>
        int OrganizationId { get; }

        /// <summary>
        /// Indicates the user can manage child objects (events/requests/tasks/itineraries) for the organization
        /// </summary>
        /// <remarks>
        /// This can be broken out into sub methods if we require discrete control over managing different type of child object (tasks/requests etc).
        /// We could also limit to action types such as delete/edit etc if our rules differ in each case. For now, this single method is enough based on our rules.
        /// </remarks>
        Task<bool> UserCanManageChildObjects();
    }
}
