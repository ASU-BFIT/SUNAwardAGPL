using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace BTS.Common.Mvc
{
    /// <summary>
    /// Chains two fields together via js
    /// </summary>
    public class Chain
    {
        /// <summary>
        /// What fields are chained to this one
        /// </summary>
        public List<string> Children { get; set; }
        /// <summary>
        /// Extra fields to include in AJAX call
        /// </summary>
        public List<string> Include { get; set; }
        /// <summary>
        /// AJAX call
        /// </summary>
        public string DataSource { get; set; }
        /// <summary>
        /// If true, disable children if we're blank/null
        /// </summary>
        public bool DisableNext { get; set; }
        /// <summary>
        /// Warn on missing chain actions
        /// </summary>
        public bool WarnMissing { get; set; }
        /// <summary>
        /// AJAX namespace
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// Call chain on pageload
        /// </summary>
        public bool UpdateOnInit { get; set; }

        /// <summary>
        /// Get HTML attributes for chain
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetAttributes()
        {
            var attrs = new Dictionary<string, object>();

            if (Children == null || Children.Count == 0)
            {
                return attrs;
            }

            attrs["data-chain"] = String.Join(",", Children);

            if (Include != null && Include.Count > 0)
            {
                attrs["data-chain-include"] = String.Join(",", Include);
            }

            // default values for DisableNext and UpdateOnInit are true and
            // default value for WarnMissing is false in the chain js,
            // so exclude these attrs from the generated HTML if they are equal to default
            if (!DisableNext)
            {
                attrs["data-chain-disable-next"] = DisableNext;
            }

            if (WarnMissing)
            {
                attrs["data-chain-warn-missing"] = WarnMissing;
            }

            if (!UpdateOnInit)
            {
                attrs["data-chain-update-on-init"] = UpdateOnInit;
            }

            if (!String.IsNullOrEmpty(Namespace))
            {
                attrs["data-chain-namespace"] = Namespace;
            }

            if (!String.IsNullOrEmpty(DataSource))
            {
                attrs["data-chain-data-source"] = DataSource;
            }

            return attrs;
        }
    }

    /// <summary>
    /// Strongly typed interface used to ensure type-safety in parameters
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IChainInclude<TModel>
    {
        /// <summary>
        /// Gets the name of the include
        /// </summary>
        /// <returns></returns>
        string GetName();
    }

    /// <summary>
    /// Chain include
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    public class ChainInclude<TModel, TProperty> : IChainInclude<TModel>
    {
        private HtmlHelper<TModel> Helper { get; set; }
        private Expression<Func<TModel, TProperty>> Include { get; set; }

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="include"></param>
        public ChainInclude(HtmlHelper<TModel> helper, Expression<Func<TModel, TProperty>> include)
        {
            Helper = helper;
            Include = include;
        }

        /// <summary>
        /// Gets name of include
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return Helper.NameFor(Include).ToString();
        }
    }
}
