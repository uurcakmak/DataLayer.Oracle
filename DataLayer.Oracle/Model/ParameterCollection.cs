using System;
using System.Collections.Generic;

namespace DataLayer.Oracle.Model
{
    public class ParameterCollection
    {
        /// <summary>
        /// Contains input parameter couples as parameterName:string, parameterValue:object for any PLSQL Stored Procedure
        /// </summary>
        public List<Tuple<string, object>> InputParameters { get; set; }

        /// <summary>
        /// Contains output parameter couples as parameterName:string, parameterValue:object for any PLSQL Stored Procedure
        /// </summary>
        public List<Tuple<string, object>> OutputParameters { get; set; } = new List<Tuple<string, object>>();

        /// <summary>
        /// Name of the PLSQL Stored Procedure
        /// </summary>
        public string CommandText { get; set; }
        
        public ParameterCollection(string commandText,
            List<Tuple<string, object>> inputParameters)
        {
            CommandText = commandText;
            InputParameters = inputParameters;
        }

        public ParameterCollection(string commandText,
            List<Tuple<string, object>> inputParameters,
            List<Tuple<string, object>> outputParameters)
        {
            CommandText = commandText;
            InputParameters = inputParameters;
            OutputParameters = outputParameters;
        }
    }
}
