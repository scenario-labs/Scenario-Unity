using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestSharp;
using Newtonsoft.Json;
using System;

namespace Scenario.Editor
{
    [ExecuteInEditMode]
    public class ScenarioSession
    {
        #region Public Fields

        public static ScenarioSession Instance = null;

        public Team LocalTeam { get { return localTeam; } set { localTeam = value; } }

        #endregion

        #region Private Fields

        private Team localTeam = null;
        
        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        public static void CreateSessions()
        {
            ApiClient.RestGet($"teams", CreateSessionsResponse);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        private static void CreateSessionsResponse(IRestResponse response)
        {
            TeamRoot teamRoot = JsonConvert.DeserializeObject<TeamRoot>(response.Content);

            if (teamRoot != null)
            {
                if (Instance == null)
                {
                    Instance = new ScenarioSession();
                }

                if (teamRoot.teams != null && teamRoot.teams.Count > 0)
                { 
                    Instance.LocalTeam = teamRoot.teams[0];
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class TeamRoot
    {
        public List<Team> teams { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Team
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string plan { get; set; }
        public string createdAt { get; set; }
        public string updateAt { get; set; }
        //public string context { get; set; }
    }
}