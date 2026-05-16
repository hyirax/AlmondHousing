using Dalamud.Bindings.ImGui;
using System.Numerics;
using Dalamud.Interface;
using AlmondHousing.Util;
using System;

namespace AlmondHousing.Gui
{
    // 🚀 partial 关键字让它与主窗口无缝合体
    public partial class ConfigurationWindow
    {
        // 🎮 魂斗罗秘籍：上 上 下 下 左 右 左 右 B A
        private readonly ImGuiKey[] _konamiCode = new ImGuiKey[]
        {
            ImGuiKey.UpArrow, ImGuiKey.UpArrow,
            ImGuiKey.DownArrow, ImGuiKey.DownArrow,
            ImGuiKey.LeftArrow, ImGuiKey.RightArrow,
            ImGuiKey.LeftArrow, ImGuiKey.RightArrow,
            ImGuiKey.B, ImGuiKey.A
        };

        // 记录玩家当前输对了几个键
        private int _konamiProgress = 0;
        
        // 记录秘籍是否解锁 (不存入 Config，每次重启游戏都要重新输，仪式感拉满)
        private bool _isDeveloperModeUnlocked = false;

        /// <summary>
        /// 隐形键盘监听器 & 复古音效引擎
        /// </summary>
        private void ListenForCheatCode()
        {
            // 如果玩家正在搜索框里打字，就不要监听，防止误触
            if (ImGui.GetIO().WantCaptureKeyboard && !ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)) 
                return;

            // 我们只关心这 6 个键
            ImGuiKey[] keysToCheck = { ImGuiKey.UpArrow, ImGuiKey.DownArrow, ImGuiKey.LeftArrow, ImGuiKey.RightArrow, ImGuiKey.B, ImGuiKey.A };
            
            foreach (var key in keysToCheck)
            {
                if (ImGui.IsKeyPressed(key))
                {
                    // 如果按下的键和秘籍当前进度的键对上了
                    if (key == _konamiCode[_konamiProgress])
                    {
                        _konamiProgress++; 
                        
                        // 如果进度满了，说明整套输入完成！
                        if (_konamiProgress >= _konamiCode.Length)
                        {
                            _isDeveloperModeUnlocked = !_isDeveloperModeUnlocked; // 切换解锁状态
                            _konamiProgress = 0; // 归零，等待下次触发
                            
                            // 🎵 播放超级极客的复古街机音效 (使用 Task.Run 防止卡顿游戏画面)
                            if (_isDeveloperModeUnlocked)
                            {
                                // 开启彩蛋：超级玛丽 1UP 升调音效 (Do -> Mi -> Sol -> 高Do)
                                System.Threading.Tasks.Task.Run(() => 
                                {
                                    System.Console.Beep(523, 100); 
                                    System.Console.Beep(659, 100); 
                                    System.Console.Beep(784, 100); 
                                    System.Console.Beep(1046, 300); 
                                });
                            }
                            else
                            {
                                // 关闭彩蛋：游戏结束降调音效 (高Do -> Sol -> Do)
                                System.Threading.Tasks.Task.Run(() => 
                                {
                                    System.Console.Beep(1046, 100);
                                    System.Console.Beep(784, 100);
                                    System.Console.Beep(523, 300);
                                });
                            }
                        }
                    }
                    else
                    {
                        // 输错了直接打回原形！(但如果按错的键正好是第一个键"上"，则算作重新开始的第一步)
                        _konamiProgress = (key == _konamiCode[0]) ? 1 : 0;
                    }
                    
                    break; // 这一帧只要处理了一个按键就够了
                }
            }
        }
    }
}