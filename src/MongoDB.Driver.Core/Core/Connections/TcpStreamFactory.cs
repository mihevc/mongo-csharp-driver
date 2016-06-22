/* Copyright 2013-2016 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a factory for a binary stream over a TCP/IP connection.
    /// </summary>
    internal class TcpStreamFactory : IStreamFactory
    {
        // fields
        private readonly TcpStreamSettings _settings;

        // constructors
        public TcpStreamFactory()
        {
            _settings = new TcpStreamSettings();
        }

        public TcpStreamFactory(TcpStreamSettings settings)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
        }

        // methods
        public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
        {
            //var socket = CreateSocket(endPoint);
            var socket = Connect(endPoint);
            return CreateNetworkStream(socket);
        }

        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var socket = CreateSocket(endPoint);
            await ConnectAsync(socket, endPoint, cancellationToken).ConfigureAwait(false);
            return CreateNetworkStream(socket);
        }

        // non-public methods
        private void ConfigureConnectedSocket(Socket socket)
        {
            socket.NoDelay = true;
            socket.ReceiveBufferSize = _settings.ReceiveBufferSize;
            socket.SendBufferSize = _settings.SendBufferSize;

            var socketConfigurator = _settings.SocketConfigurator;
            if (socketConfigurator != null)
            {
                socketConfigurator(socket);
            }
        }

        private Socket Connect(EndPoint endPoint)
        {
            var dnsEndPoint = endPoint as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                // UNIX and Linux don't support multiple tries on the same socket so .netcore sends us back a platform not supported exception if
                //   we pass in the host.
                var addresses = Dns.GetHostAddressesAsync(dnsEndPoint.Host).GetAwaiter().GetResult();

                Exception lastExc = null;
                foreach (var address in addresses)
                {
                    var s = new Socket(GetAddressFamily(endPoint), SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        s.ConnectAsync(address, dnsEndPoint.Port).GetAwaiter().GetResult();
                        return s;
                    }
                    catch (Exception ex)
                    {
                        lastExc = ex;
                    }
                }

                if (lastExc != null) throw lastExc;
                throw new Exception("Was unable to connect on any addresses");
            }
            else
            {
                var socket = new Socket(GetAddressFamily(endPoint), SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(endPoint);
                return socket;
            }
        }

        private async Task ConnectAsync(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            var connected = false;
            var cancelled = false;
            var timedOut = false;

            using (var registration = cancellationToken.Register(() => { if (!connected) { cancelled = true; try { socket.Dispose(); } catch { } } }))
            using (var timer = new Timer(_ => { if (!connected) { timedOut = true; try { socket.Dispose(); } catch { } } }, null, _settings.ConnectTimeout, Timeout.InfiniteTimeSpan))
            {
                try
                {
                    var dnsEndPoint = endPoint as DnsEndPoint;
                    if (dnsEndPoint != null)
                    {
                        // mono doesn't support DnsEndPoint in its BeginConnect method.
                        await socket.ConnectAsync(dnsEndPoint.Host, dnsEndPoint.Port).ConfigureAwait(false);
                        //await Task.Factory.FromAsync(socket.BeginConnect(dnsEndPoint.Host, dnsEndPoint.Port, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
                    else
                    {
                        await socket.ConnectAsync(endPoint).ConfigureAwait(false);
                        //await Task.Factory.FromAsync(socket.BeginConnect(endPoint, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
                    connected = true;
                    return;
                }
                catch
                {
                    if (!cancelled && !timedOut)
                    {
                        throw;
                    }
                }
            }

            if (socket.Connected)
            {
                try { socket.Dispose(); } catch { }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (timedOut)
            {
                var message = string.Format("Timed out connecting to {0}. Timeout was {1}.", endPoint, _settings.ConnectTimeout);
                throw new TimeoutException(message);
            }
        }

        private NetworkStream CreateNetworkStream(Socket socket)
        {
            ConfigureConnectedSocket(socket);

            var stream = new NetworkStream(socket, true);

            if (_settings.ReadTimeout.HasValue)
            {
                var readTimeout = (int)_settings.ReadTimeout.Value.TotalMilliseconds;
                if (readTimeout != 0)
                {
                    stream.ReadTimeout = readTimeout;
                }
            }

            if (_settings.WriteTimeout.HasValue)
            {
                var writeTimeout = (int)_settings.WriteTimeout.Value.TotalMilliseconds;
                if (writeTimeout != 0)
                {
                    stream.WriteTimeout = writeTimeout;
                }
            }

            return stream;
        }

        private AddressFamily GetAddressFamily(EndPoint endPoint)
        {
            var addressFamily = endPoint.AddressFamily;
            return addressFamily == AddressFamily.Unspecified || addressFamily == AddressFamily.Unknown
                ? _settings.AddressFamily
                : addressFamily;
        }

        private Socket CreateSocket(EndPoint endPoint)
        {
            var addressFamily = endPoint.AddressFamily;
            if (addressFamily == AddressFamily.Unspecified || addressFamily == AddressFamily.Unknown)
            {
                addressFamily = _settings.AddressFamily;
            }
            return new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
