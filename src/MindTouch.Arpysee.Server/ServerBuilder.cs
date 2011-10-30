/*
 * MindTouch.Arpysee
 * 
 * Copyright (C) 2011 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/MindTouch.Arpysee
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
using MindTouch.Arpysee.Server.Async;
using MindTouch.Arpysee.Server.Sync;

namespace MindTouch.Arpysee.Server {
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

        private ServerBuilder(IPEndPoint endPoint, bool isAsync) {
            _endPoint = endPoint;
            _clientHandlerFactory = isAsync
                ? (IClientHandlerFactory)new AsyncClientHandlerFactory(_asyncRepository)
                : new SyncClientHandlerFactory(_syncRepository);
        }

        public ArpyseeServer Build() {
            return new ArpyseeServer(_endPoint, _clientHandlerFactory);
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
           return new AsyncFluentCommandRegistration(this,_asyncRepository,command);
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
    }
}