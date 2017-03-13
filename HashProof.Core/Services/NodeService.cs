using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using nStratis;
using nStratis.Protocol;
using nStratis.Protocol.Payloads;

namespace HashProof.Core.Services
{
    public class NodeConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
    public class NodeService
    {
        private readonly NodeConfig _config;
        private Node _node;
        public NodeService(NodeConfig config)
        {
            _config = config;

            if (string.IsNullOrEmpty(_config.Host))
            {
                _config.Host = "127.0.0.1";
            }

            if ( _config.Port <= 0)
            {
                _config.Port = 16178;
            }

            Init();
        }

        private void Init()
        {
            _node = Node.Connect(Network.Main, new IPEndPoint(IPAddress.Parse(_config.Host), _config.Port));
            if (_node.IsConnected)
            {
                _node.MessageReceived += _node_MessageReceived;
                _node.Disconnected += _node_Disconnected;

                
            }
            else
            {
                throw new Exception("Couldn't connect to the node ");
                
            }
        }

        private void _node_Disconnected(Node node)
        {
            throw new NotImplementedException();
        }

        private void _node_MessageReceived(Node node, IncomingMessage message)
        {
            throw new NotImplementedException();
        }

        public void SyncBlocks(string blockId)
        {
            
        }
    }
}
