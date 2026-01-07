using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using windowOP;

namespace RemoteDesktopServer
{
    public class RemoteDesktopHandler : IDisposable
    {
        private readonly WebSocket _webSocket;
        private readonly AesCrypto _aesCrypto;
        private readonly ScreenCapturer _screenCapturer;
        private readonly InputSimulator _inputSimulator;
        private bool _disposed;

        public RemoteDesktopHandler(WebSocket webSocket, string aesKey)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _screenCapturer = new ScreenCapturer();
            _inputSimulator = new InputSimulator();

            // 生成随机IV并初始化加密组件
            var iv = GenerateSecureIV();
            _aesCrypto = new AesCrypto(
                key: NormalizeAesKey(aesKey),
                iv: iv
            );

            // 立即发送IV给客户端
            SendIVAsync(iv).ConfigureAwait(false);
        }

        public async Task StartHandlingAsync()
        {
            try
            {
                DatabaseOP.Log($"新的远程桌面连接，ID: {Guid.NewGuid().ToString()[^4..]}");
                DatabaseOP.Log("IV已发送");

                var tasks = new List<Task>
            {
                ReceiveInputCommandsAsync(),
                SendScreenDataAsync()
            };

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                DatabaseOP.LogErr($"处理失败: {ex.Message}");
                DatabaseOP.LogErr(ex.StackTrace);
            }
        }

        private async Task SendScreenDataAsync()
        {
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var sw = Stopwatch.StartNew();
                    using var image = _screenCapturer.CaptureScreen();
                    // DatabaseOP.Log($"屏幕捕获完成，尺寸: {image.Width}x{image.Height}");
                    //DatabaseOP.Log($"捕获完成，耗时: {sw.ElapsedMilliseconds}ms");

                    using var ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Jpeg);
                    //  DatabaseOP.Log($"JPEG编码完成，大小: {ms.Length} bytes");
                    //DatabaseOP.Log($"编码完成，耗时: {sw.ElapsedMilliseconds}ms");

                    var encrypted = _aesCrypto.Encrypt(ms.ToArray());
                    //  DatabaseOP.Log($"加密完成，加密后大小: {encrypted.Length} bytes");
                    //DatabaseOP.Log($"加密完成，耗时: {sw.ElapsedMilliseconds}ms");

                    await _webSocket.SendAsync(
                        new ArraySegment<byte>(encrypted),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None);

                    //DatabaseOP.Log($"数据发送完成，耗时: {sw.ElapsedMilliseconds}ms");
                    //var bounds = Screen.PrimaryScreen.Bounds;
                    //DatabaseOP.Log($"捕获分辨率: {bounds.Width} x {bounds.Height}");
                    //image.Save("test.jpg", ImageFormat.Jpeg);
                    //await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                DatabaseOP.LogErr($"屏幕数据发送异常: {ex.Message}");
            }
        }

        private async Task ReceiveInputCommandsAsync()
        {
            var buffer = new byte[4096];
            try
            {
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseConnectionAsync();
                        break;
                    }

                    var decryptedData = _aesCrypto.Decrypt(buffer.AsSpan(0, result.Count).ToArray());
                    ProcessInputCommand(decryptedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"输入命令接收失败: {ex.Message}");
            }
        }

        private async Task CloseConnectionAsync()
        {
            try
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "正常关闭",
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭连接时出错: {ex.Message}");
            }
        }

        private void ProcessInputCommand(byte[] data)
        {
            if (data.Length < 4) return; // 至少需要4字节的命令类型

            var commandType = BitConverter.ToInt32(data, 0); // 从索引0开始读取4字节的命令类型

            switch (commandType)
            {
                case 0x01: // 鼠标移动 (X, Y)
                           // 预期数据长度: 命令类型(4) + X(4) + Y(4) = 12 字节
                    if (data.Length < 12) return;
                    var moveX = BitConverter.ToInt32(data, 4); // X 坐标从索引4开始
                    var moveY = BitConverter.ToInt32(data, 8); // Y 坐标从索引8开始
                    _inputSimulator.MoveMouse(moveX, moveY);
                    break;

                case 0x02: // 鼠标点击/抬起 (X, Y, ButtonState)
                           // 客户端发送数据长度: 命令类型(4) + X(4) + Y(4) + ButtonState(1) = 13 字节
                           // 这里的 data.Length 应该是解密后的数据长度，也就是 Payload 的长度 (CommandHeader + MouseData)
                           // 所以，这里应该判断 data.Length >= 4 (命令头) + 4 (X) + 4 (Y) + 1 (ButtonState) = 13
                    if (data.Length < 13)
                    {
                        // 可以添加日志来调试实际接收到的数据长度
                        DatabaseOP.LogErr($"鼠标点击命令数据长度不足. 预期: 13, 实际: {data.Length}");
                        return;
                    }
                    // 客户端发送的数据：4字节命令头，接着是 4字节X, 4字节Y, 1字节ButtonState
                    var clickX = BitConverter.ToInt32(data, 4); // X 坐标从解密后数据的索引4开始
                    var clickY = BitConverter.ToInt32(data, 8); // Y 坐标从解密后数据的索引8开始
                    var buttonState = data[12]; // ButtonState 从解密后数据的索引12开始

                    // 注意：你原始的 HandleMouseClick 并没有接收 X, Y 参数，这会导致点击位置不准确
                    // 需要修改 _inputSimulator.HandleMouseClick 方法来接收 X, Y
                    _inputSimulator.HandleMouseClick(clickX, clickY, buttonState); // 传入 X, Y 和 ButtonState
                    break;

                case 0x03: // 键盘事件 (KeyCode, IsDown)
                           // 客户端发送数据长度: 命令类型(4) + KeyCode(2) + IsDown(1) = 7 字节
                    if (data.Length < 7)
                    {
                        DatabaseOP.LogErr($"键盘命令数据长度不足. 预期: 7, 实际: {data.Length}");
                        return;
                    }
                    var keyCode = BitConverter.ToInt16(data, 4); // KeyCode 从解密后数据的索引4开始
                    var keyDown = data[6] == 1; // IsDown 从解密后数据的索引6开始
                    _inputSimulator.SimulateKeyEvent((VirtualKeyCode)keyCode, keyDown);
                    break;

                case 0x05: // 鼠标滚轮 (X, Y, Delta)
                           // 客户端发送数据长度: 命令类型(4) + X(4) + Y(4) + DeltaY(4) = 16 字节
                    if (data.Length < 16)
                    {
                        DatabaseOP.LogErr($"鼠标滚轮命令数据长度不足. 预期: 16, 实际: {data.Length}");
                        return;
                    }
                    var scrollX = BitConverter.ToInt32(data, 4);
                    var scrollY = BitConverter.ToInt32(data, 8);
                    var deltaY = BitConverter.ToInt32(data, 12);
                    _inputSimulator.SimulateMouseWheel(scrollX, scrollY, deltaY); // 需要在 InputSimulator 中实现
                    break;
            }
        }

        private async Task SendIVAsync(byte[] iv)
        {
            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(iv),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IV发送失败: {ex.Message}");
                throw;
            }
        }

        private static byte[] GenerateSecureIV()
        {
            using var rng = RandomNumberGenerator.Create();
            var iv = new byte[16];
            rng.GetBytes(iv);
            return iv;
        }

        private static byte[] NormalizeAesKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("AES密钥不能为空");

            // 先将字符串转换为字节数组
            var keyBytes = Encoding.UTF8.GetBytes(key);

            // 处理字节数组长度
            if (keyBytes.Length < 16)
            {
                // 使用字节数组填充
                Array.Resize(ref keyBytes, 16);
            }
            else if (keyBytes.Length > 16)
            {
                // 截取前16字节
                var temp = new byte[16];
                Array.Copy(keyBytes, temp, 16);
                keyBytes = temp;
            }

            return keyBytes;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _webSocket.Dispose();
            _aesCrypto.Dispose();
            _screenCapturer.Dispose();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }

    public class AesCrypto : IDisposable
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        public AesCrypto(byte[] key, byte[] iv)
        {
            _aes = Aes.Create();
            _aes.Key = key;
            _aes.IV = iv;
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        public byte[] Encrypt(byte[] plainData)
        {
            return _encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
        }

        public byte[] Decrypt(byte[] cipherData)
        {
            return _decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
        }

        public void Dispose()
        {
            _encryptor.Dispose();
            _decryptor.Dispose();
            _aes.Dispose();
        }
    }

    public class ScreenCapturer : IDisposable
    {
        private bool _disposed;
        private readonly int _targetWidth;
        private readonly int _targetHeight;

        // 可选：通过构造函数指定目标分辨率
        public ScreenCapturer(int targetWidth = 1920, int targetHeight = 1080)
        {
            _targetWidth = targetWidth;
            _targetHeight = targetHeight;
        }

        public Bitmap CaptureScreen()
        {
            var bounds = Screen.PrimaryScreen.Bounds;

            // 先捕获全屏
            using var fullBitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var graphics = Graphics.FromImage(fullBitmap))
            {
                graphics.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            // 缩放到目标分辨率（1080p）
            var scaledBitmap = new Bitmap(_targetWidth, _targetHeight);
            using (var g = Graphics.FromImage(scaledBitmap))
            {
                // 使用低质量但快速的插值模式
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                g.DrawImage(fullBitmap, 0, 0, _targetWidth, _targetHeight);
            }

            return scaledBitmap;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    public class InputSimulator
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        public void MoveMouse(int x, int y)
        {
            var screenWidth = Screen.PrimaryScreen.Bounds.Width;
            var screenHeight = Screen.PrimaryScreen.Bounds.Height;

            var normalizedX = (int)(x * (65535.0 / screenWidth));
            var normalizedY = (int)(y * (65535.0 / screenHeight));

            mouse_event(0x8001, normalizedX, normalizedY, 0, 0);
        }

        public void HandleMouseClick(int x, int y, int buttonState)
        {
            const int MOUSEEVENTF_LEFTDOWN = 0x02;
            const int MOUSEEVENTF_LEFTUP = 0x04;
            const int MOUSEEVENTF_RIGHTDOWN = 0x08;
            const int MOUSEEVENTF_RIGHTUP = 0x10;
            // 注意：这里不需要 MOUSEEVENTF_MOVE 和 MOUSEEVENTF_ABSOLUTE，因为 MoveMouse 已经处理了定位
            // 但是如果点击和移动是同时发生的，mouse_event 可能需要合并标志
            // 但为了分离职责，先 MoveMouse 再 MouseEvent 是一个可行的方法。

            // 先移动到指定位置
            MoveMouse(x, y); // 复用 MoveMouse 函数

            // 然后执行点击操作
            switch (buttonState)
            {
                case 0x01: // 左键按下
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    // 不需要同时发送抬起，因为客户端是分开发送按下和抬起事件的
                    break;
                case 0x02: // 右键按下
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    // 不需要同时发送抬起
                    break;
                case 0x04: // 左键抬起
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case 0x08: // 右键抬起
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
            }
        }
        // 为鼠标滚轮事件添加 DllImport (dwData 参数类型不同)
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

        const int MOUSEEVENTF_WHEEL = 0x0800; // 滚轮事件标志
        const int WHEEL_DELTA = 120; // 滚轮滚动单位，Windows 默认值

        public void SimulateMouseWheel(int x, int y, int deltaY)
        {
            // 移动到滚轮事件发生的位置 (可选，但推荐，确保滚轮操作发生在预期区域)
            MoveMouse(x, y);

            // 将客户端的 deltaY 转换为 Windows 的滚轮单位
            // 客户端的 e.deltaY 通常是 +/-100 或 +/-3 左右，
            // 而 Windows 需要 WHEEL_DELTA 的倍数
            int wheelAmount = (int)(deltaY / 100.0 * WHEEL_DELTA); // 假设客户端每次滚动是 100

            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, wheelAmount, UIntPtr.Zero);
        }


        public void SimulateKeyEvent(VirtualKeyCode keyCode, bool keyDown)
        {
            const int KEYEVENTF_EXTENDEDKEY = 0x0001;
            const int KEYEVENTF_KEYUP = 0x0002;

            var flags = keyDown ? 0 : KEYEVENTF_KEYUP;
            keybd_event((byte)keyCode, 0, flags | KEYEVENTF_EXTENDEDKEY, 0);
        }
    }

    public enum VirtualKeyCode : ushort
    {
        LEFT_BUTTON = 0x01,
        RIGHT_BUTTON = 0x02,
        CANCEL = 0x03,
        BACKSPACE = 0x08,
        TAB = 0x09,
        ENTER = 0x0D,
        SHIFT = 0x10,
        CONTROL = 0x11,
        ALT = 0x12,
        ESCAPE = 0x1B,
        SPACE = 0x20,
        // 可根据需要添加其他键码...
    }

    
}