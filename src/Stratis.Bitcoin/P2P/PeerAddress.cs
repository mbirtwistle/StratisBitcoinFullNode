﻿using System;
using System.Net;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Stratis.Bitcoin.P2P
{
    /// <summary>
    /// A class which holds data on a peer's (IPEndPoint) attempts, connections and successful handshake events.
    /// </summary>
    [JsonObject]
    public sealed class PeerAddress
    {
        /// <summary>EndPoint of this peer.</summary>
        [JsonProperty(PropertyName = "endpoint")]
        [JsonConverter(typeof(IPEndPointConverter))]
        private IPEndPoint endpoint;

        /// <summary>Used to construct the <see cref="NetworkAddress"/> after deserializing this peer.</summary>
        [JsonProperty(PropertyName = "addressTime", NullValueHandling = NullValueHandling.Ignore)]
        private DateTimeOffset? addressTime;

        /// <summary>The <see cref="NetworkAddress"/> of this peer.</summary>
        [JsonIgnore]
        public NetworkAddress NetworkAddress
        {
            get
            {
                if (this.endpoint == null)
                    return null;

                var networkAddress = new NetworkAddress(this.endpoint);
                if (this.addressTime != null)
                    networkAddress.Time = this.addressTime.Value;

                return networkAddress;
            }
        }

        /// <summary>The source address of this peer.</summary>
        [JsonProperty(PropertyName = "loopback")]
        private string loopback;

        [JsonIgnore]
        public IPAddress Loopback
        {
            get
            {
                if (string.IsNullOrEmpty(this.loopback))
                    return null;
                return IPAddress.Parse(this.loopback);
            }
        }

        /// <summary>
        /// The amount of connection attempts.
        /// <para>
        /// This gets reset when a connection was successful.</para>
        /// </summary>
        [JsonProperty(PropertyName = "connectionAttempts")]
        public int ConnectionAttempts { get; private set; }

        /// <summary>
        /// The last successful version handshake.
        /// <para>
        /// This is set when the connection attempt was successful and a handshake was done.
        /// </para>
        /// </summary>
        [JsonProperty(PropertyName = "lastConnectionHandshake", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastConnectionHandshake { get; private set; }

        /// <summary>
        /// <c>True</c> if the peer has had connection attempts but none successful.
        /// </summary>
        [JsonIgnore]
        public bool Attempted
        {
            get
            {
                return
                    (this.LastConnectionAttempt != null) &&
                    (this.LastConnectionSuccess == null) &&
                    (this.LastConnectionHandshake == null);
            }
        }

        /// <summary>
        /// <c>True</c> if the peer has had a successful connection attempt.
        /// </summary>
        [JsonIgnore]
        public bool Connected
        {
            get
            {
                return
                    (this.LastConnectionAttempt == null) &&
                    (this.LastConnectionSuccess != null) &&
                    (this.LastConnectionHandshake == null);
            }
        }

        /// <summary>
        /// <c>True</c> if the peer has never had connection attempts.
        /// </summary>
        [JsonIgnore]
        public bool Fresh
        {
            get
            {
                return
                    (this.LastConnectionAttempt == null) &&
                    (this.LastConnectionSuccess == null) &&
                    (this.LastConnectionHandshake == null);
            }
        }

        /// <summary>
        /// <c>True</c> if the peer has had a successful connection attempt and handshaked.
        /// </summary>
        [JsonIgnore]
        public bool Handshaked
        {
            get
            {
                return
                    (this.LastConnectionAttempt == null) &&
                    (this.LastConnectionSuccess != null) &&
                    (this.LastConnectionHandshake != null);
            }
        }

        /// <summary>
        /// The last connection attempt.
        /// <para>
        /// This is set regardless of whether or not the connection attempt was successful or not.
        /// </para>
        /// </summary>
        [JsonProperty(PropertyName = "lastConnectionAttempt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastConnectionAttempt { get; private set; }

        /// <summary>
        /// The last successful connection attempt.
        /// <para>
        /// This is set when the connection attempt was successful (but not necessarily handshaked).
        /// </para>
        /// </summary>
        [JsonProperty(PropertyName = "lastConnectionSuccess", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? LastConnectionSuccess { get; private set; }

        /// <summary>
        /// Increments <see cref="ConnectionAttempts"/> and sets the <see cref="LastConnectionAttempt"/>.
        /// </summary>
        internal void SetAttempted(DateTimeOffset peerAttemptedAt)
        {
            this.ConnectionAttempts += 1;
            this.LastConnectionAttempt = peerAttemptedAt;
            this.LastConnectionSuccess = null;
            this.LastConnectionHandshake = null;
        }

        /// <summary>
        /// Sets the <see cref="LastConnectionSuccess"/>, <see cref="addressTime"/> and <see cref="NetworkAddress.Time"/> properties.
        /// <para>
        /// Resets <see cref="ConnectionAttempts"/> and <see cref="LastConnectionAttempt"/>.
        /// </para>
        /// </summary>
        internal void SetConnected(DateTimeOffset peerConnectedAt)
        {
            this.addressTime = peerConnectedAt;
            this.NetworkAddress.Time = peerConnectedAt;

            this.LastConnectionAttempt = null;
            this.ConnectionAttempts = 0;

            this.LastConnectionSuccess = peerConnectedAt;
        }

        /// <summary>Sets the <see cref="LastConnectionHandshake"/> date.</summary>
        internal void SetHandshaked(DateTimeOffset peerHandshakedAt)
        {
            this.LastConnectionHandshake = peerHandshakedAt;
        }

        /// <summary>
        /// Creates a new peer address instance.
        /// </summary>
        /// <param name="address">The network address of the peer.</param>
        public static PeerAddress Create(NetworkAddress address)
        {
            return new PeerAddress
            {
                ConnectionAttempts = 0,
                endpoint = address.Endpoint,
                loopback = IPAddress.Loopback.ToString()
            };
        }

        /// <summary>
        /// Creates a new peer address instance and sets the loopback address (source).
        /// </summary>
        /// <param name="address">The network address of the peer.</param>
        /// <param name="loopback">The loopback (source) of the peer.</param>
        public static PeerAddress Create(NetworkAddress address, IPAddress loopback)
        {
            var peer = Create(address);
            peer.loopback = loopback.ToString();
            return peer;
        }
    }

    /// <summary>
    /// Converter used to convert <see cref="IPEndPoint"/> to and from JSON.
    /// </summary>
    /// <seealso cref="JsonConverter" />
    public sealed class IPEndPointConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPEndPoint);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var json = JToken.Load(reader).ToString();
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var endPointComponents = json.Split('|');
            return new IPEndPoint(IPAddress.Parse(endPointComponents[0]), Convert.ToInt32(endPointComponents[1]));
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IPEndPoint ipEndPoint)
            {
                if (ipEndPoint.Address != null || ipEndPoint.Port != 0)
                {
                    JToken.FromObject(string.Format("{0}|{1}", ipEndPoint.Address, ipEndPoint.Port)).WriteTo(writer);
                    return;
                }
            }

            writer.WriteNull();
        }
    }
}