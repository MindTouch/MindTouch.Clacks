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
using System.Collections.Generic;

namespace MindTouch.Arpysee.Server.Async {
    public interface IAsyncFluentCommandRegistration {
        IAsyncFluentCommandRegistration IsDisconnect();
        IAsyncFluentCommandRegistration HandledBy(Func<IRequest, IResponse> handler);
        IAsyncFluentCommandRegistration HandledBy(Func<IRequest, IEnumerable<IResponse>> handler);
        IAsyncFluentCommandRegistration HandledBy(Action<IRequest, Action<IResponse>> handler);
        IAsyncFluentCommandRegistration HandledBy(Action<IRequest, Action<IResponse, Action>> handler);
        IAsyncFluentCommandRegistration HandledBy(Action<IRequest, Action<IEnumerable<IResponse>>> handler);
        IAsyncFluentCommandRegistration ExpectsData();
        IAsyncFluentCommandRegistration ExpectsNoData();
        IAsyncServerBuilder Register();
    }
}