using System;
using jp.ootr.common;

namespace jp.ootr.ImageDeviceController
{
    public enum URLStoreSyncAction
    {
        AddUrl,
        SyncAll,
        None
    }

    public enum LoadError
    {
        Unknown,
        InvalidURL,
        AccessDenied,
        DownloadError,
        InvalidImage,
        TooManyRequests,

        //以下独自エラー
        InsecureURL,
        DuplicateURL,
        URLNotSynced,

        //UNITY ERROR
        HostNotFound,
        RedirectNotAllowed,
        TooLarge,

        //UdonZip
        MissingUdonZip,
        InvalidZipFile,
        InvalidManifest,
        InvalidMetadata,
        UnsupportedManifestVersion,
        UnsupportedFeature,

        //VideoLoader
        MissingVRCAVProVideoPlayer,
        LiveVideoNotSupported,
        PlayerError,
        RateLimited,
        InvalidOptions,

        //EIALoader
        MissingUdonLZ4,
        InvalidEIAFile,

        //StringLoader
        UnknownFileFormat,

        // FileLoadError
        InvalidFileURL,

        //HTTP1.1 4xx
        HttpBadRequest = 400,
        HttpUnauthorized = 401,
        HttpPaymentRequired = 402,
        HttpForbidden = 403,
        HttpNotFound = 404,
        HttpMethodNotAllowed = 405,
        HttpNotAcceptable = 406,
        HttpProxyAuthenticationRequired = 407,
        HttpRequestTimeout = 408,
        HttpConflict = 409,
        HttpGone = 410,
        HttpLengthRequired = 411,
        HttpPreconditionFailed = 412,
        HttpPayloadTooLarge = 413,
        HttpURITooLong = 414,
        HttpUnsupportedMediaType = 415,
        HttpRangeNotSatisfiable = 416,
        HttpExpectationFailed = 417,
        HttpImATeapot = 418,
        HttpMisdirectedRequest = 421,
        HttpUnprocessableEntity = 422,
        HttpLocked = 423,
        HttpFailedDependency = 424,
        HttpTooEarly = 425,
        HttpUpgradeRequired = 426,
        HttpPreconditionRequired = 428,
        HttpTooManyRequests = 429,
        HttpRequestHeaderFieldsTooLarge = 431,
        HttpUnavailableForLegalReasons = 451,

        //HTTP1.1 5xx
        HttpInternalServerError = 500,
        HttpNotImplemented = 501,
        HttpBadGateway = 502,
        HttpServiceUnavailable = 503,
        HttpGatewayTimeout = 504,
        HttpHTTPVersionNotSupported = 505,
        HttpVariantAlsoNegotiates = 506,
        HttpInsufficientStorage = 507,
        HttpLoopDetected = 508,
        HttpNotExtended = 510,
        HttpNetworkAuthenticationRequired = 511
    }

    public enum SourceType
    {
        Image,
        StringKind,
        Video,
        Local,
        Unknown,

        [Obsolete("Use StringKind instead", false)]
        TextZip = 1,
    }

    public static class LoadErrorExtensions
    {
        public static string GetString(this LoadError error)
        {
            switch (error)
            {
                case LoadError.Unknown:
                    return "VRC:Unknown";
                case LoadError.InvalidURL:
                    return "VRC:InvalidURL";
                case LoadError.AccessDenied:
                    return "VRC:AccessDenied";
                case LoadError.DownloadError:
                    return "VRC:DownloadError";
                case LoadError.InvalidImage:
                    return "VRC:InvalidImage";
                case LoadError.TooManyRequests:
                    return "VRC:TooManyRequests";

                case LoadError.HostNotFound:
                    return "Unity:HostNotFound";
                case LoadError.RedirectNotAllowed:
                    return "Unity:RedirectNotAllowed";
                case LoadError.TooLarge:
                    return "Unity:TooLarge";

                case LoadError.URLNotSynced:
                    return "ootr:URLNotSynced";
                case LoadError.InsecureURL:
                    return "ootr:InsecureURL";
                case LoadError.DuplicateURL:
                    return "ootr:DuplicateURL";
                case LoadError.MissingUdonZip:
                    return "ootr:MissingUdonZip";
                case LoadError.InvalidZipFile:
                    return "ootr:InvalidZipFile";

                case LoadError.PlayerError:
                    return "VRC:PlayerError";
                case LoadError.RateLimited:
                    return "VRC:RateLimited";

                case LoadError.InvalidManifest:
                    return "ootr:UdonZip:InvalidManifest";
                case LoadError.InvalidMetadata:
                    return "ootr:UdonZip:InvalidMetadata";
                case LoadError.UnsupportedManifestVersion:
                    return "ootr:UdonZip:UnsupportedManifestVersion";
                case LoadError.UnsupportedFeature:
                    return "ootr:UdonZip:UnsupportedFeature";

                case LoadError.MissingVRCAVProVideoPlayer:
                    return "ootr:Video:MissingVRCAVProVideoPlayer";
                case LoadError.LiveVideoNotSupported:
                    return "ootr:Video:LiveVideoNotSupported";
                case LoadError.InvalidOptions:
                    return "ootr:Video:InvalidOptions";

                case LoadError.MissingUdonLZ4:
                    return "ootr:EIA:MissingUdonLZ4";
                case LoadError.InvalidEIAFile:
                    return "ootr:EIA:InvalidEIAFile";

                case LoadError.UnknownFileFormat:
                    return "ootr:String:UnknownFileFormat";

                case LoadError.InvalidFileURL:
                    return "ootr:Local:InvalidFileURL";

                case LoadError.HttpBadRequest:
                    return "HTTP/1.1 400 Bad Request";
                case LoadError.HttpUnauthorized:
                    return "HTTP/1.1 401 Unauthorized";
                case LoadError.HttpPaymentRequired:
                    return "HTTP/1.1 402 Payment Required";
                case LoadError.HttpForbidden:
                    return "HTTP/1.1 403 Forbidden";
                case LoadError.HttpNotFound:
                    return "HTTP/1.1 404 Not Found";
                case LoadError.HttpMethodNotAllowed:
                    return "HTTP/1.1 405 Method Not Allowed";
                case LoadError.HttpNotAcceptable:
                    return "HTTP/1.1 406 Not Acceptable";
                case LoadError.HttpProxyAuthenticationRequired:
                    return "HTTP/1.1 407 Proxy Authentication Required";
                case LoadError.HttpRequestTimeout:
                    return "HTTP/1.1 408 Request Timeout";
                case LoadError.HttpConflict:
                    return "HTTP/1.1 409 Conflict";
                case LoadError.HttpGone:
                    return "HTTP/1.1 410 Gone";
                case LoadError.HttpLengthRequired:
                    return "HTTP/1.1 411 Length Required";
                case LoadError.HttpPreconditionFailed:
                    return "HTTP/1.1 412 Precondition Failed";
                case LoadError.HttpPayloadTooLarge:
                    return "HTTP/1.1 413 Payload Too Large";
                case LoadError.HttpURITooLong:
                    return "HTTP/1.1 414 URI Too Long";
                case LoadError.HttpUnsupportedMediaType:
                    return "HTTP/1.1 415 Unsupported Media Type";
                case LoadError.HttpRangeNotSatisfiable:
                    return "HTTP/1.1 416 Range Not Satisfiable";
                case LoadError.HttpExpectationFailed:
                    return "HTTP/1.1 417 Expectation Failed";
                case LoadError.HttpImATeapot:
                    return "HTCPCP/1.0 418 I'm a teapot";
                case LoadError.HttpMisdirectedRequest:
                    return "HTTP/1.1 421 Misdirected Request";
                case LoadError.HttpUnprocessableEntity:
                    return "HTTP/1.1 422 Unprocessable Entity";
                case LoadError.HttpLocked:
                    return "HTTP/1.1 423 Locked";
                case LoadError.HttpFailedDependency:
                    return "HTTP/1.1 424 Failed Dependency";
                case LoadError.HttpTooEarly:
                    return "HTTP/1.1 425 Too Early";
                case LoadError.HttpUpgradeRequired:
                    return "HTTP/1.1 426 Upgrade Required";
                case LoadError.HttpPreconditionRequired:
                    return "HTTP/1.1 428 Precondition Required";
                case LoadError.HttpTooManyRequests:
                    return "HTTP/1.1 429 Too Many Requests";
                case LoadError.HttpRequestHeaderFieldsTooLarge:
                    return "HTTP/1.1 431 Request Header Fields Too Large";
                case LoadError.HttpUnavailableForLegalReasons:
                    return "HTTP/1.1 451 Unavailable For Legal Reasons";
                case LoadError.HttpInternalServerError:
                    return "HTTP/1.1 500 Internal Server Error";
                case LoadError.HttpNotImplemented:
                    return "HTTP/1.1 501 Not Implemented";
                case LoadError.HttpBadGateway:
                    return "HTTP/1.1 502 Bad Gateway";
                case LoadError.HttpServiceUnavailable:
                    return "HTTP/1.1 503 Service Unavailable";
                case LoadError.HttpGatewayTimeout:
                    return "HTTP/1.1 504 Gateway Timeout";
                case LoadError.HttpHTTPVersionNotSupported:
                    return "HTTP/1.1 505 HTTP Version Not Supported";
                case LoadError.HttpVariantAlsoNegotiates:
                    return "HTTP/1.1 506 Variant Also Negotiates";
                case LoadError.HttpInsufficientStorage:
                    return "HTTP/1.1 507 Insufficient Storage";
                case LoadError.HttpLoopDetected:
                    return "HTTP/1.1 508 Loop Detected";
                case LoadError.HttpNotExtended:
                    return "HTTP/1.1 510 Not Extended";
                case LoadError.HttpNetworkAuthenticationRequired:
                    return "HTTP/1.1 511 Network Authentication Required";
                default:
                    return $"<Unknown Error:Code {error}>";
            }
        }


        public static void ParseMessage(this LoadError error, out string title, out string content)
        {
            switch (I18n.GetSystemLanguage())
            {
                case Language.JaJp:
                    ParseErrorMessageJaJp(error, out title, out content);
                    break;
                default:
                    ParseErrorMessageEnUs(error, out title, out content);
                    break;
            }
        }

        private static void ParseErrorMessageJaJp(LoadError error, out string title, out string content)
        {
            switch (error)
            {
                case LoadError.Unknown:
                    title = "未知のエラーが発生しました";
                    content = "別の画像を試すか、時間をおいて再度試してみてください";
                    break;
                case LoadError.InvalidURL:
                    title = "URLが正しくありません";
                    content = "入力したURLが合っているか再度確認してみてください";
                    break;
                case LoadError.AccessDenied:
                    title = "アクセスが拒否されました";
                    content = "Allow untrusted URLsを有効にしてみてください";
                    break;
                case LoadError.DownloadError:
                    title = "ダウンロードに失敗しました";
                    content = "時間をおいて再度試してみてください";
                    break;
                case LoadError.InvalidImage:
                    title = "非対応形式の画像です";
                    content = "リンク先が画像であっているか、画像が2048x2048以下に収まっているか確認してみてください";
                    break;
                case LoadError.TooManyRequests:
                    title = "リクエストが多すぎます";
                    content = "時間をおいて再度試してみてください";
                    break;
                case LoadError.InsecureURL:
                    title = "安全でないURLです";
                    content = "\"http://\"で始まるURLは許可されていません。\"https://\"で始まるURLを使用してください";
                    break;
                case LoadError.DuplicateURL:
                    title = "重複したURLです";
                    content = "同じURLは登録できません";
                    break;
                case LoadError.URLNotSynced:
                    title = "URLが同期されませんでした";
                    content = "時間をおいて再度試してみてください";
                    break;
                case LoadError.HostNotFound:
                    title = "ホストが見つかりません";
                    content = "URLが正しいか確認してください";
                    break;
                case LoadError.RedirectNotAllowed:
                    title = "リダイレクトは許可されていません";
                    content = "リダイレクト後のURLを入力してみてください";
                    break;
                case LoadError.TooLarge:
                    title = "ファイルサイズが大きすぎます";
                    content = "ファイルサイズが100MB以下に収まっているか確認してください";
                    break;
                case LoadError.MissingUdonZip:
                    title = "UdonZipコンポーネントが見つかりません";
                    content = "ワールド制作者に問い合わせてください";
                    break;
                case LoadError.InvalidZipFile:
                case LoadError.InvalidManifest:
                case LoadError.InvalidMetadata:
                    title = "無効なTextZipファイル形式です";
                    content = "再度試すか、アップロードし直してみてください";
                    break;
                case LoadError.UnsupportedManifestVersion:
                    title = "TextZipのバージョンがサポートされていません";
                    content = "ワールド制作者にギミックの更新を依頼するか、ターゲットバージョンを変更してアップロードし直してください";
                    break;
                case LoadError.UnsupportedFeature:
                    title = "TextZipの機能がサポートされていません";
                    content = "ワールド制作者にギミックの更新を依頼するか、ターゲットバージョンを変更してアップロードし直してください";
                    break;
                case LoadError.MissingVRCAVProVideoPlayer:
                    title = "VRCAVProVideoPlayerコンポーネントが見つかりません";
                    content = "ワールド制作者に問い合わせてください";
                    break;
                case LoadError.LiveVideoNotSupported:
                    title = "ライブ動画はサポートされていません";
                    content = "ワールド制作者に問い合わせてください";
                    break;
                case LoadError.PlayerError:
                    title = "動画プレイヤーでエラーが発生しました";
                    content = "動画のURLが正しいか、動画がサポートされている形式であるか確認してください";
                    break;
                case LoadError.RateLimited:
                    title = "リクエストが多すぎます";
                    content = "時間をおいて再度試してみてください";
                    break;
                case LoadError.InvalidOptions:
                    title = "無効なオプションです";
                    content = "動画のオプションが正しいか確認してください";
                    break;
                case LoadError.MissingUdonLZ4:
                    title = "UdonLZ4コンポーネントが見つかりません";
                    content = "ワールド制作者に問い合わせてください";
                    break;
                case LoadError.InvalidEIAFile:
                    title = "無効なEIAファイル形式です";
                    content = "再度試すか、アップロードし直してみてください";
                    break;
                case LoadError.UnknownFileFormat:
                    title = "無効なファイル形式です";
                    content = "ファイルがサポートされているフォーマットか確認してください";
                    break;
                case LoadError.InvalidFileURL:
                    title = "無効なファイルURLです";
                    content = "ファイルのURLが正しいか確認してください";
                    break;
                default:
                    if ((int)error > 300)
                    {
                        title = "HTTPエラーが発生しました";
                        content = "エラーコード: " + error.GetString();
                        break;
                    }

                    title = "未知のエラーです";
                    content = "エラーコード: " + error.GetString();
                    break;
            }
        }

        private static void ParseErrorMessageEnUs(LoadError error, out string title, out string content)
        {
            switch (error)
            {
                case LoadError.Unknown:
                    title = "Unknown error occurred";
                    content = "Try another image or try again later";
                    break;
                case LoadError.InvalidURL:
                    title = "Invalid URL";
                    content = "Check if the URL is correct";
                    break;
                case LoadError.AccessDenied:
                    title = "Access denied";
                    content = "Try enabling Allow untrusted URLs";
                    break;
                case LoadError.DownloadError:
                    title = "Download failed";
                    content = "Try again later";
                    break;
                case LoadError.InvalidImage:
                    title = "Invalid image";
                    content = "Check if the link is an image or if the image is within 2048x2048";
                    break;
                case LoadError.TooManyRequests:
                    title = "Too many requests";
                    content = "Try again later";
                    break;
                case LoadError.InsecureURL:
                    title = "Insecure URL";
                    content =
                        "URLs starting with \"http://\" are not allowed. Use URLs starting with \"https://\" instead";
                    break;
                case LoadError.DuplicateURL:
                    title = "Duplicate URL";
                    content = "The same URL cannot be registered";
                    break;
                case LoadError.URLNotSynced:
                    title = "URL not synced";
                    content = "Try again later";
                    break;
                case LoadError.HostNotFound:
                    title = "Host not found";
                    content = "Check if the URL is correct";
                    break;
                case LoadError.RedirectNotAllowed:
                    title = "Redirect not allowed";
                    content = "Enter the URL after the redirect";
                    break;
                case LoadError.TooLarge:
                    title = "File size too large";
                    content = "Check if the file size is within 100MB";
                    break;
                case LoadError.MissingUdonZip:
                    title = "UdonZip component not found";
                    content = "Please contact the world creator";
                    break;
                case LoadError.InvalidZipFile:
                case LoadError.InvalidManifest:
                case LoadError.InvalidMetadata:
                    title = "Invalid TextZip file format";
                    content = "Please try again or re-upload";
                    break;
                case LoadError.UnsupportedManifestVersion:
                    title = "TextZip version not supported";
                    content = "Please ask the world creator to update the gimmick or change the target version and re-upload";
                    break;
                case LoadError.UnsupportedFeature:
                    title = "TextZip feature not supported";
                    content = "Please ask the world creator to update the gimmick or change the target version and re-upload";
                    break;
                case LoadError.MissingVRCAVProVideoPlayer:
                    title = "VRCAVProVideoPlayer component not found";
                    content = "Please contact the world creator";
                    break;
                case LoadError.LiveVideoNotSupported:
                    title = "Live video not supported";
                    content = "Please contact the world creator";
                    break;
                case LoadError.PlayerError:
                    title = "An error occurred in the video player";
                    content = "Please verify that the video URL is correct and that the video is in a supported format";
                    break;
                case LoadError.RateLimited:
                    title = "Too many requests";
                    content = "Please try again later";
                    break;
                case LoadError.InvalidOptions:
                    title = "Invalid options";
                    content = "Please verify that the video options are correct";
                    break;
                case LoadError.MissingUdonLZ4:
                    title = "UdonLZ4 component not found";
                    content = "Please contact the world creator";
                    break;
                case LoadError.InvalidEIAFile:
                    title = "Invalid EIA file format";
                    content = "Please try again or re-upload";
                    break;
                case LoadError.UnknownFileFormat:
                    title = "Invalid file format";
                    content = "Please check if the file is in a supported format";
                    break;
                case LoadError.InvalidFileURL:
                    title = "Invalid file URL";
                    content = "Please check if the file URL is correct";
                    break;
                default:
                    if ((int)error > 300)
                    {
                        title = "HTTP error occurred";
                        content = "Error code: " + error.GetString();
                        break;
                    }

                    title = "Unknown error";
                    content = "Error code: " + error.GetString();
                    break;
            }
        }
    }
}
