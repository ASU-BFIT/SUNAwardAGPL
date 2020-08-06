using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTS.Common.Mvc.Grid
{
    /// <summary>
    /// Represents the type of column in a grid
    /// </summary>
    public enum ColumnType
    {
        /// <summary>
        /// Column is shown by default but can be hidden by the user
        /// </summary>
        DefaultVisible = 0,
        /// <summary>
        /// Column is hidden by default but can be shown by the user
        /// </summary>
        DefaultHidden = 1,
        /// <summary>
        /// Column is always shown and cannot be hidden by the user
        /// </summary>
        AlwaysVisible = 2,
        /// <summary>
        /// Column is always hidden and cannot be shown by the user
        /// </summary>
        AlwaysHidden = 3,
        /// <summary>
        /// As AlwaysVisible, but the column cannot be reordered and will always appear on the leftmost side of the grid
        /// </summary>
        Fixed = 6
    }

    [Flags]
    internal enum ColumnFlags
    {
        Hidden = 1,
        NotCustomizable = 2,
        NotMovable = 4
    }

    internal static class ColumnTypeExtensions
    {
        internal static bool HasFlags(this ColumnType type, ColumnFlags flags)
        {
            return ((int)type & (int)flags) == (int)flags;
        }
    }
}
