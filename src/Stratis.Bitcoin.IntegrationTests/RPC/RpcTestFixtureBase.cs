﻿using System;
using System.IO;
using NBitcoin.RPC;
using Stratis.Bitcoin.P2P.Peer;

namespace Stratis.Bitcoin.IntegrationTests.RPC
{
    /// <summary>
    /// Abstract base class for RPC Test Fixtures for both Bitcoin and Stratis networks.
    /// </summary>
    public abstract class RpcTestFixtureBase : IDisposable
    {
        /// <summary>Node builder for the test fixture.</summary>
        protected NodeBuilder Builder { get; set; }

        /// <summary>The node for the test fixture.</summary>
        public CoreNode Node { get; protected set; }

        /// <summary>The RPC client for the test fixture.</summary>
        public RPCClient RpcClient { get; protected set; }

        /// <summary>The network peer client for the test fixture.</summary>
        public NetworkPeer NetworkPeerClient { get; protected set; }

        /// <summary>
        /// Constructs the test fixture by calling initialize which should initialize the properties of the fixture.
        /// </summary>
        public RpcTestFixtureBase()
        {
            this.InitializeFixture();
        }

        /// <summary>
        /// Initializes the test fixtures properties as approriate for the network.
        /// </summary>
        protected abstract void InitializeFixture();

        /// <summary>
        /// Disposes of the test fixtures resources.
        /// Note: do not call this dispose in the class itself xunit will handle it. 
        /// </summary>
        public void Dispose()
        {
            this.Builder.Dispose();
            this.NetworkPeerClient.Dispose();
        }

        /// <summary>
        /// Copies the test wallet into data folder for node if it isnt' already present.
        /// </summary>
        /// <param name="node">Core node for the test.</param>
        protected void InitializeTestWallet(CoreNode node)
        {
            string testWalletPath = Path.Combine(node.DataFolder, "test.wallet.json");
            if (!File.Exists(testWalletPath))
                File.Copy("Data/test.wallet.json", testWalletPath);
        }
    }
}
