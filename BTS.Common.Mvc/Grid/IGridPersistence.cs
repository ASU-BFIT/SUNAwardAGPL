using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Wrapper to persist per-user grid settings
    /// </summary>
    public interface IGridPersistence
    {
        /// <summary>
        /// Loads the options for the given grid from storage.
        /// </summary>
        /// <param name="user">User to load grid options for</param>
        /// <param name="gridName">Name of the grid</param>
        /// <returns>JSON string that was previously saved by Save()</returns>
        string Load(IPrincipal user, string gridName);

        /// <summary>
        /// Saves the options for the given grid into storage.
        /// </summary>
        /// <param name="user">User to save grid options for</param>
        /// <param name="gridName">Name of the grid</param>
        /// <param name="options">JSON string containing options to save</param>
        void Save(IPrincipal user, string gridName, string options);

        /// <summary>
        /// Deletes the saved options for the given grid from storage.
        /// </summary>
        /// <param name="user">User to delete grid options for</param>
        /// <param name="gridName">Name of the grid</param>
        void Delete(IPrincipal user, string gridName);
    }
}
