using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using NFormatting = Newtonsoft.Json.Formatting;

namespace Microsoft.ReportViewer.Common.FrontLookCode
{
    public static class FL_DataParser
    {
        /// <summary>
        /// Converts a JSON string to an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to convert to.</typeparam>
        /// <param name="t">The JSON string to convert.</param>
        /// <returns>The converted object.</returns>
        public static T CastToClass<T>(this string t) where T : class
        {
            try
            {
                //Dont use readonly keyword for settings
                //DeserializeObject only objects with get; and set; properties

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        SerializeCompilerGeneratedMembers = true,
                        IgnoreSerializableAttribute = true
                    },
                    //Formatting = Formatting.Indented,
                    //PreserveReferencesHandling = PreserveReferencesHandling.All,
                };
                return JsonConvert.DeserializeObject<T>(t, settings);
            }
            catch (Exception ex)
            {
                return default;
            }
        }

        /// <summary>
        /// Converts an object to a JSON string.
        /// </summary>
        /// <param name="t">The object to convert.</param>
        /// <param name="IndentedFormating">Whether to use indented formatting.</param>
        /// <param name="_PreserveReferencesHandling">Specifies reference loop handling options for the JsonSerializer.</param>
        /// <param name="UseDefaultMode">Whether to use default mode.</param>
        /// <returns>The JSON string.</returns>
        public static string CastToJson<T>(this T t, bool IndentedFormating = false, PreserveReferencesHandling? _PreserveReferencesHandling = null, bool UseDefaultMode = false) where T : class
        {
            if (UseDefaultMode)
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = IndentedFormating ? NFormatting.Indented : NFormatting.None,
                    PreserveReferencesHandling = _PreserveReferencesHandling ?? PreserveReferencesHandling.None,
                };

                return JsonConvert.SerializeObject(t, settings);
            }
            else
            {
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        SerializeCompilerGeneratedMembers = true,
                        IgnoreSerializableAttribute = true,
                    },

                    //Formatting = Formatting.Indented,
                    //PreserveReferencesHandling = PreserveReferencesHandling.All,
                };

                if (IndentedFormating)
                {
                    settings.Formatting = NFormatting.Indented;
                }
                if (_PreserveReferencesHandling != null)
                {
                    settings.PreserveReferencesHandling = _PreserveReferencesHandling.Value;
                }

                return JsonConvert.SerializeObject(t, settings);
            }
        }
    }

}
