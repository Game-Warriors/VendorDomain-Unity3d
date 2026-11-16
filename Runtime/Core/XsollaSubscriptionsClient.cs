using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWarriors.VendorDomian.Core
{
#if XSOLLA || true
    /// <summary>
    /// Xsolla subscription plans are separate from the SDK's Virtual Items catalog.
    /// These endpoints require the player's Xsolla Login JWT, not a publisher API key.
    /// </summary>
    internal sealed class XsollaSubscriptionsClient
    {
        private const string BaseUrl = "https://subscriptions.xsolla.com/api/user/v1/projects/";
        private readonly string _projectUrl;
        private readonly Func<Task<string>> _getAccessToken;
        private readonly bool _sandbox;

        internal XsollaSubscriptionsClient(int projectId, Func<Task<string>> getAccessToken, bool sandbox)
        {
            _projectUrl = BaseUrl + projectId;
            _getAccessToken = getAccessToken;
            _sandbox = sandbox;
        }

        internal async Task<List<Plan>> GetPlans()
        {
            var result = new List<Plan>();
            for (var offset = 0; offset < 5000; offset += 50)
            {
                var page = await Request<PlanPage>($"{_projectUrl}/plans?limit=50&offset={offset}", "GET");
                if (page.items != null)
                    result.AddRange(page.items);
                if (!page.has_more)
                    return result;
            }

            throw new InvalidOperationException("Xsolla returned too many subscription plan pages.");
        }

        internal async Task<List<Subscription>> GetSubscriptions()
        {
            var result = new List<Subscription>();
            for (var offset = 0; offset < 5000; offset += 50)
            {
                var page = await Request<SubscriptionPage>($"{_projectUrl}/subscriptions?limit=50&offset={offset}", "GET");
                if (page.items != null)
                    result.AddRange(page.items);
                if (!page.has_more)
                    return result;
            }

            throw new InvalidOperationException("Xsolla returned too many subscription pages.");
        }

        internal async Task<string> GetCheckoutUrl(string planExternalId)
        {
            if (string.IsNullOrWhiteSpace(planExternalId))
                throw new ArgumentException("A subscription plan external ID is required.", nameof(planExternalId));
            if (_sandbox)
                throw new InvalidOperationException("Xsolla's client subscription checkout cannot request a sandbox token. Use a server-generated sandbox Pay Station token for test purchases.");

            var body = JsonUtility.ToJson(new BuyRequest { plan_external_id = planExternalId });
            var url = $"{_projectUrl}/subscriptions/buy";
            var result = await Request<BuyResponse>(url, "POST", body);
            if (!Uri.TryCreate(result.link_to_ps, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("Xsolla did not return a valid HTTPS subscription checkout link.");
            return uri.AbsoluteUri;
        }

        private async Task<T> Request<T>(string url, string method, string body = null) where T : class
        {
            var token = await _getAccessToken();
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Xsolla Login did not provide a user JWT for subscriptions.");

            using (var request = new UnityWebRequest(url, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = 20;
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + token);

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException($"Xsolla subscriptions request failed (HTTP {request.responseCode}): {request.error}");

                var response = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(response))
                    throw new InvalidOperationException("Xsolla returned an empty subscriptions response.");

                var parsed = JsonUtility.FromJson<T>(response);
                return parsed ?? throw new InvalidOperationException("Xsolla returned an invalid subscriptions response.");
            }
        }

        [Serializable]
        internal sealed class Plan
        {
            public string plan_external_id = string.Empty;
            public string plan_name = string.Empty;
            public string plan_description = string.Empty;
            public Charge charge = new Charge();
        }

        [Serializable]
        internal sealed class Charge
        {
            public float amount = 0f;
            public float setup_fee = 0f;
            public string currency = string.Empty;
        }

        [Serializable]
        internal sealed class Subscription
        {
            public long id = 0;
            public string plan_external_id = string.Empty;
            public string status = string.Empty;
            public string date_next_charge = string.Empty;
            public string date_create = string.Empty;
        }

        [Serializable]
        private sealed class PlanPage
        {
            public Plan[] items = Array.Empty<Plan>();
            public bool has_more = false;
        }

        [Serializable]
        private sealed class SubscriptionPage
        {
            public Subscription[] items = Array.Empty<Subscription>();
            public bool has_more = false;
        }

        [Serializable]
        private sealed class BuyRequest
        {
            public string plan_external_id = string.Empty;
        }

        [Serializable]
        private sealed class BuyResponse
        {
            public string link_to_ps = string.Empty;
        }
    }
#endif
}
