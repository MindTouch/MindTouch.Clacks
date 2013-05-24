using System;
using System.Text;
using MindTouch.Clacks.Server.Async;
using MindTouch.Clacks.Server.Sync;

namespace MindTouch.Clacks.Server {
    public static class DefaultHandlers {
        private static readonly Logger.ILog _log = Logger.CreateLog();

        public static IResponse ErrorHandler(IRequest request, Exception e) {
            _log.Warn(string.Format("Request [{0}] threw an exception of type {1}: {2}", request.Command, e.GetType(), e.Message), e);
            return Response.Create("ERROR").WithArgument(e.GetType()).WithData(Encoding.UTF8.GetBytes(e.Message));
        }

        public static ISyncCommandRegistration SyncCommandRegistration {
            get {
                return new SyncCommandRegistration(
                    DataExpectation.Auto,
                    (client, cmd, dataLength, arguments, errorHandler) =>
                        new SyncSingleCommandHandler(client, cmd, arguments, dataLength, DefaultResponse, errorHandler)
                );
            }
        }

        public static IResponse DisconnectHandler(IRequest request) {
            return Response.Create("BYE");
        }

        public static void ErrorHandler(IRequest request, Exception e, Action<IResponse> responseCallback) {
            var response = ErrorHandler(request, e);
            responseCallback(response);
        }

        public static IAsyncCommandRegistration AsyncCommandRegistration {
            get {
                return new AsyncCommandRegistration(
                    DataExpectation.Auto,
                    (client, command, dataLength, arguments, errorHandler) =>
                        new AsyncSingleCommandHandler(client, command, arguments, dataLength, (r, c) => c(DefaultResponse(r)), errorHandler)
                );
            }
        }

        private static IResponse DefaultResponse(IRequest request) {
            return Response.Create("UNKNOWN");
        }

        public static void DisconnectHandler(IRequest request, Action<IResponse> responseCallback) {
            responseCallback(DisconnectHandler(request));
        }
    }
}
