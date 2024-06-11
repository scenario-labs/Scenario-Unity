using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RestSharp;
using Newtonsoft.Json;
using System;

namespace Scenario.Editor
{
    /// <summary>
    /// Register localy a session of the scenario plugin and api information
    /// </summary>
    public class ScenarioSession
    {
        #region Public Fields

        public static ScenarioSession Instance = null;

        public Team LocalTeam { get { return localTeam; } set { localTeam = value; } }

        #endregion

        #region Private Fields

        /// <summary>
        /// Register the team id and information of the account connected
        /// </summary>
        private Team localTeam = null;

        /// <summary>
        /// Register the limit reference object from api and the team id connected
        /// </summary>
        private LimitRoot limitRoot = null;

        /// <summary>
        /// Register limits plan according to the account connected
        /// </summary>
        private Limit limits = null;
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Create a Scenario Session thanks to api connection.
        /// </summary>
        public static void CreateSessions()
        {
            ApiClient.RestGet($"teams", CreateSessionsResponse);
        }

        /// <summary>
        /// Get the plan register on this account
        /// </summary>
        /// <returns> Return the plan </returns>
        public string GetPlan()
        {
            if (Instance != null)
            {
                if (Instance.limitRoot != null)
                {
                    if (!string.IsNullOrEmpty(Instance.limitRoot.plan))
                    { 
                        return Instance.limitRoot.plan;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get limit batch inference allow to this account
        /// </summary>
        /// <returns> Return the inference limit </returns>
        public int GetInferenceBatchSize()
        {
            if (Instance != null)
            {
                if (Instance.limitRoot != null)
                {
                    if (!string.IsNullOrEmpty(Instance.limitRoot.limits.inferenceBatchSize))
                    {
                        return int.Parse(Instance.limitRoot.limits.inferenceBatchSize);
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Return the parallel inference limit
        /// </summary>
        /// <returns></returns>
        public int GetParallelInference()
        {
            if (Instance != null)
            {
                if (Instance.limitRoot != null)
                {
                    if (!string.IsNullOrEmpty(Instance.limitRoot.limits.parallelInferences))
                    {
                        return int.Parse(Instance.limitRoot.limits.parallelInferences);
                    }
                }
            }

            return -2;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Response to the creation of the session get result and register it.
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
                    ApiClient.RestGet($"teams/{teamRoot.teams[0].id}/limits", GetAccountLimit);
                }
            }
        }

        /// <summary>
        /// After creating a session get plan and limits of the account connected.
        /// </summary>
        /// <param name="_response"></param>
        private static void GetAccountLimit(IRestResponse _response)
        {
            LimitRoot limitRoot = JsonConvert.DeserializeObject<LimitRoot>(_response.Content);

            if(limitRoot != null) 
            {
                if (Instance == null)
                { 
                    Instance = new ScenarioSession();
                }

                if (limitRoot.limits != null && !string.IsNullOrEmpty(limitRoot.plan))
                {
                    Instance.limitRoot = limitRoot;
                    Instance.limits = limitRoot.limits;
                }
            }
        }

        #endregion
    }

    #region API_DTO

    /// <summary>
    /// Team Root object from api request
    /// </summary>
    [Serializable]
    public class TeamRoot
    {
        public List<Team> teams { get; set; }
    }

    /// <summary>
    /// Team object from api request
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

    /// <summary>
    /// Limit Root Object from api request
    /// </summary>
    [Serializable]
    public class LimitRoot
    { 
        public string plan { get; set; }
        public Limit limits { get; set; }
    }

    /// <summary>
    /// Limit Object from api request.
    /// </summary>
    [Serializable]
    public class Limit
    {
        public string inferenceBatchSize { get; set; }
        public string creativeUnits { get; set; }
        public string parallelInferences { get; set; }
    }

    #endregion
}