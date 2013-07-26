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
using MindTouch.Clacks.Server.Async;
using MindTouch.Clacks.Server.Sync;

namespace MindTouch.Clacks.Server {
    public class ServerBuilder : ISyncServerBuilder, IAsyncServerBuilder {

        public static ISyncServerBuilder CreateSync(IPEndPoint endPoint) {
            return new ServerBuilder(endPoint, false);
        }

        public static IAsyncServerBuilder CreateAsync(IPEndPoint endPoint) {
            return new ServerBuilder(endPoint, true);
        }

        private readonly IPEndPoint _endPoint;
        private readonly IClientHandlerFactory _clientHandlerFactory;
        private readonly AsyncCommandRepository _asyncRepository = new AsyncCommandRepository();
        private readonly SyncCommandRepository _syncRepository = new SyncCommandRepository();
        private InstrumentationBuilder _instrumenation;

        private ServerBuilder(IPEndPoint endPoint, bool isAsync) {
            _endPoint = endPoint;
            _clientHandlerFactory = isAsync
                ? (IClientHandlerFactory)new AsyncClientHandlerFactory(_asyncRepository)
                : new SyncClientHandlerFactory(_syncRepository);
        }

        public ClacksServer Build() {
            return Build(_instrumenation);
        }

        public ClacksServer Build(IClacksInstrumentation instrumentation) {
            return new ClacksServer(_endPoint, instrumentation ?? BaseClacksInstrumentation.Instance, _clientHandlerFactory);
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnClientConnected(Action<Connection> clientConnected) {
            EnsureInstrumentation();
            _instrumenation.OnClientConnected(clientConnected);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnClientDisconnected(Action<Connection> clientDisconnected) {
            EnsureInstrumentation();
            _instrumenation.OnClientDisconnected(clientDisconnected);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnCommandCompleted(Action<StatsCommandInfo> commandCompleted) {
            EnsureInstrumentation();
            _instrumenation.OnCommandCompleted(commandCompleted);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnAwaitingCommand(Action<StatsCommandInfo> awaitingCommand) {
            EnsureInstrumentation();
            _instrumenation.OnAwaitingCommand(awaitingCommand);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnProcessedCommand(Action<StatsCommandInfo> processedCommand) {
            EnsureInstrumentation();
            _instrumenation.OnProcessedCommand(processedCommand);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnReceivedCommand(Action<StatsCommandInfo> receivedCommand) {
            EnsureInstrumentation();
            _instrumenation.OnReceivedCommand(receivedCommand);
            return this;
        }

        IAsyncServerBuilder IServerBuilder<IAsyncServerBuilder>.OnReceivedCommandPayload(Action<StatsCommandInfo> receivedCommandPayload) {
            EnsureInstrumentation();
            _instrumenation.OnReceivedCommandPayload(receivedCommandPayload);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnClientConnected(Action<Connection> clientConnected) {
            EnsureInstrumentation();
            _instrumenation.OnClientConnected(clientConnected);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnClientDisconnected(Action<Connection> clientDisconnected) {
            EnsureInstrumentation();
            _instrumenation.OnClientDisconnected(clientDisconnected);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnCommandCompleted(Action<StatsCommandInfo> commandCompleted) {
            EnsureInstrumentation();
            _instrumenation.OnCommandCompleted(commandCompleted);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnAwaitingCommand(Action<StatsCommandInfo> awaitingCommand) {
            EnsureInstrumentation();
            _instrumenation.OnAwaitingCommand(awaitingCommand);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnProcessedCommand(Action<StatsCommandInfo> processedCommand) {
            EnsureInstrumentation();
            _instrumenation.OnProcessedCommand(processedCommand);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnReceivedCommand(Action<StatsCommandInfo> receivedCommand) {
            EnsureInstrumentation();
            _instrumenation.OnReceivedCommand(receivedCommand);
            return this;
        }

        ISyncServerBuilder IServerBuilder<ISyncServerBuilder>.OnReceivedCommandPayload(Action<StatsCommandInfo> receivedCommandPayload) {
            EnsureInstrumentation();
            _instrumenation.OnReceivedCommandPayload(receivedCommandPayload);
            return this;
        }

        public IAsyncServerBuilder WithDefaultHandler(Func<IRequest, IResponse> handler) {
            _asyncRepository.Default((request, responseCallback) => responseCallback(handler(request)));
            return this;
        }

        public IAsyncServerBuilder WithErrorHandler(Func<IRequest, Exception, IResponse> handler) {
            _asyncRepository.Error((request, error, responseCallback) => responseCallback(handler(request, error)));
            return this;
        }

        IAsyncServerBuilder IAsyncServerBuilder.WithDefaultHandler(Action<IRequest, Action<IResponse>> handler) {
            _asyncRepository.Default(handler);
            return this;
        }

        IAsyncServerBuilder IAsyncServerBuilder.WithErrorHandler(Action<IRequest, Exception, Action<IResponse>> handler) {
            _asyncRepository.Error(handler);
            return this;
        }

        IAsyncFluentCommandRegistration IAsyncServerBuilder.WithCommand(string command) {
            return new AsyncFluentCommandRegistration(this, _asyncRepository, command);
        }

        ISyncServerBuilder ISyncServerBuilder.WithDefaultHandler(Func<IRequest, IResponse> handler) {
            _syncRepository.Default(handler);
            return this;
        }

        ISyncServerBuilder ISyncServerBuilder.WithErrorHandler(Func<IRequest, Exception, IResponse> handler) {
            _syncRepository.Error(handler);
            return this;
        }

        ISyncFluentCommandRegistration ISyncServerBuilder.WithCommand(string command) {
            return new SyncFluentCommandRegistration(this, _syncRepository, command);
        }

        private void EnsureInstrumentation() {
            if(_instrumenation == null) {
                _instrumenation = new InstrumentationBuilder();
            }
        }
    }
}