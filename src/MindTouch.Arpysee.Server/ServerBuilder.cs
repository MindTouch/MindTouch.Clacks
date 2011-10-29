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

namespace MindTouch.Arpysee.Server {
    public class ServerBuilder : IServerBuilder {

        public static IServerBuilder Configure(IPEndPoint endPoint) {
            return new ServerBuilder(endPoint);
        }

        private readonly IPEndPoint _endPoint;
        private IClientHandlerFactory _clientHandlerFactory = new AsyncClientHandlerFactory();
        private readonly CommandRepository _repository = new CommandRepository();
        private ServerBuilder(IPEndPoint endPoint) {
            _endPoint = endPoint;
        }

        public ArpyseeServer Build() {
            return new ArpyseeServer(_endPoint,_repository,_clientHandlerFactory);
        }

        public IServerBuilder UseSyncIO() {
            return UseAsyncIO(false);
        }

        public IServerBuilder UseAsyncIO() {
            return UseAsyncIO(true);
        }

        public IServerBuilder UseAsyncIO(bool useAsync) {
             _clientHandlerFactory = useAsync ? (IClientHandlerFactory)new AsyncClientHandlerFactory(): new SyncClientHandlerFactory();
            return this;
        }

        public IServerBuilder WithCommands(Action<ICommandRegistry> registry) {
            registry(_repository);
            return this;
        }
    }
}