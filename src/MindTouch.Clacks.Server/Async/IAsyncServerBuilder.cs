/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011 Arne F. Claassen
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

namespace MindTouch.Clacks.Server.Async {
    public interface IAsyncServerBuilder {
        ClacksServer Build();
        IAsyncServerBuilder WithDefaultHandler(Func<IRequest, IResponse> handler);
        IAsyncServerBuilder WithErrorHandler(Func<IRequest, Exception, IResponse> handler);
        IAsyncServerBuilder WithDefaultHandler(Action<IRequest, Action<IResponse>> handler);
        IAsyncServerBuilder WithErrorHandler(Action<IRequest, Exception, Action<IResponse>> handler);
        IAsyncFluentCommandRegistration WithCommand(string command);
    }
}