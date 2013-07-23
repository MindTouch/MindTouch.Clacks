/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011-2013 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/MindTouch.Clacks
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Net;

namespace MindTouch.Clacks.Server {
    public class InstrumentationBuilder : IClacksInstrumentation {

        private Action<Guid, IPEndPoint> _clientConnected;
        private Action<Guid, IPEndPoint> _clientDisconnected;
        private Action<StatsCommandInfo> _commandCompleted;
        private Action<Guid, ulong> _awaitingCommand;
        private Action<StatsCommandInfo> _processedCommand;
        private Action<StatsCommandInfo> _receivedCommand;
        private Action<StatsCommandInfo> _receivedCommandPayload;
 
        public void OnClientConnected(Action<Guid, IPEndPoint> clientConnected) {
            _clientConnected = clientConnected;
        }
        public void OnClientDisconnected(Action<Guid, IPEndPoint> clientDisconnected) {
            _clientDisconnected = clientDisconnected;
        }
        public void OnCommandCompleted(Action<StatsCommandInfo> commandCompleted) {
            _commandCompleted = commandCompleted;
        }
        public void OnAwaitingCommand(Action<Guid, ulong> awaitingCommand) {
            _awaitingCommand = awaitingCommand;
        }
        public void OnProcessedCommand(Action<StatsCommandInfo> processedCommand) {
            _processedCommand = processedCommand;
        }
        public void OnReceivedCommand(Action<StatsCommandInfo> receivedCommand) {
            _receivedCommand = receivedCommand;
        }
        public void OnReceivedCommandPayload(Action<StatsCommandInfo> receivedCommandPayload) {
            _receivedCommandPayload = receivedCommandPayload;
        }

        void IClacksInstrumentation.ClientConnected(Guid clientId, IPEndPoint remoteEndPoint) {
            if(_clientConnected == null) {
                return;
            }
            _clientConnected(clientId, remoteEndPoint);
        }

        void IClacksInstrumentation.ClientDisconnected(Guid clientId, IPEndPoint remoteEndPoint) {
            if(_clientDisconnected == null) {
                return;
            }
            _clientDisconnected(clientId, remoteEndPoint);
        }

        void IClacksInstrumentation.CommandCompleted(StatsCommandInfo statsCommandInfo) {
            if(_commandCompleted == null) {
                return;
            }
            _commandCompleted(statsCommandInfo);
        }

        void IClacksInstrumentation.AwaitingCommand(Guid clientId, ulong requestId) {
            if(_awaitingCommand == null) {
                return;
            }
            _awaitingCommand(clientId, requestId);
        }

         void IClacksInstrumentation.ProcessedCommand(StatsCommandInfo statsCommandInfo) {
            if(_processedCommand == null) {
                return;
            }
            _processedCommand(statsCommandInfo);
        }

        void IClacksInstrumentation.ReceivedCommand(StatsCommandInfo statsCommandInfo) {
            if(_receivedCommand == null) {
                return;
            }
            _receivedCommand(statsCommandInfo);
        }

        public void ReceivedCommandPayload(StatsCommandInfo statsCommandInfo) {
            if(_receivedCommandPayload == null) {
                return;
            }
            _receivedCommandPayload(statsCommandInfo);
        }
    }
}