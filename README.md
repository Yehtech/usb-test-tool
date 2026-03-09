# USB Suspend/Resume Tester v1.1.0

針對 USB 裝置進行持續性 Suspend/Resume 循環測試的桌面工具，用於驗證裝置在反覆掛起/恢復後的穩定性。

## 系統需求

- Windows 10 / 11
- .NET 8.0 SDK（開發用）或 .NET 8.0 Runtime（執行 EXE 用）
- **管理員權限**（pnputil 裝置控制必要）

## 快速開始

### 方法一：直接執行
雙擊 `run_usb_suspend_tester.bat`，或在命令列執行：
```
dotnet run --project USBSuspendTester
```
程式會自動偵測是否具備管理員權限，若無則彈出 UAC 提升權限視窗。

### 方法二：打包為 EXE
雙擊 `build_usb_suspend_tester.bat`，產出檔案位於 `publish\USBSuspendTester.exe`。

## 使用說明

1. 啟動程式後，上方 **USB Devices** 區域會自動列出所有已連接且帶 VID/PID 的 USB 裝置
2. 點擊 **Refresh** 可重新掃描裝置
3. 從列表中選取一個目標 USB 裝置（建議選擇非關鍵裝置，如外接鍵盤、滑鼠以外的裝置）
4. 設定測試參數：
   | 參數 | 說明 | 預設值 |
   |------|------|--------|
   | Suspend Delay | 送出 Disable 後等待多久再驗證狀態 | 2.0 秒 |
   | Resume Delay | 送出 Enable 後等待多久再驗證狀態 | 2.0 秒 |
   | Interval | 每個 Cycle 之間的間隔 | 1.0 秒 |
   | Max Cycles | 最大測試次數（0 = 無限） | 0 |
5. 按下 **START TEST** 開始測試
6. 觀察下方 **Test Log** 即時日誌
7. 按下 **STOP TEST** 停止測試（程式會自動將裝置恢復至啟用狀態）

## 測試流程（每個 Cycle）

```
Disable Device (Suspend)
    ↓
等待 Suspend Delay

    ↓
驗證裝置狀態 = Disabled
    ↓
Enable Device (Resume)
    ↓
等待 Resume Delay
    ↓
驗證裝置狀態 = Started
    ↓
記錄 PASS / FAIL
    ↓
等待 Cycle Interval → 下一輪
```

## 日誌標籤說明

| 標籤 | 顏色 | 說明 |
|------|------|------|
| `[INFO]` | 藍色 | 一般資訊（Cycle 開始、操作進行中） |
| `[PASS]` | 綠色 | 操作成功（Suspend OK / Resume OK / Cycle PASSED） |
| `[FAIL]` | 紅色 | 操作失敗（pnputil 錯誤、Cycle FAILED） |
| `[WARN]` | 黃色 | 警告（狀態未確認、裝置重新啟用中） |
| `[SYS ]` | 紫色 | 系統訊息（測試開始/結束、達到最大次數） |

日誌可透過 **Export** 按鈕匯出為 `.txt` 檔案。

## 專案結構

```
USB SUSPEND - C#/
├── USBSuspendTester.sln             # Solution 檔案
├── run_usb_suspend_tester.bat
├── build_usb_suspend_tester.bat
└── USBSuspendTester/
    ├── Program.cs                   # 進入點：管理員權限檢查 + UAC 提升
    ├── Constants.cs                 # 常數定義（顏色、預設值、pnputil 指令）
    ├── USBSuspendTester.csproj      # 專案設定（.NET 8.0 WinForms）
    ├── app.manifest                 # UAC 管理員權限宣告
    ├── Models/
    │   ├── CycleResult.cs           # 單次 Cycle 測試結果
    │   ├── TestConfig.cs            # 測試參數設定
    │   ├── TestEvent.cs             # 測試事件類型
    │   ├── TestMessage.cs           # 執行緒間訊息傳遞
    │   ├── TestStats.cs             # 測試統計數據
    │   └── USBDevice.cs             # USB 裝置資料模型
    ├── Services/
    │   ├── USBManager.cs            # USB 裝置列舉與控制（pnputil 封裝）
    │   └── TestRunner.cs            # 背景執行緒測試迴圈
    └── UI/
        ├── MainForm.cs              # 主視窗（組合三個 Panel + 訊息輪詢）
        ├── DevicePanel.cs           # USB 裝置列表（DataGridView 表格）
        ├── ControlPanel.cs          # 測試參數與 Start/Stop 控制
        └── LogPanel.cs              # 即時彩色日誌面板
```

## 技術細節

| 項目 | 方案 | 原因 |
|------|------|------|
| USB 控制 | `pnputil /disable-device` + `/enable-device` | Windows 內建，無需額外驅動 |
| 裝置列舉 | `pnputil /enum-devices /connected /class USB` | 結構化輸出，支援多語系 |
| GUI 框架 | WinForms (.NET 8.0) | 原生 Windows UI，無第三方依賴 |
| 執行緒通訊 | `ConcurrentQueue<TestMessage>` | Thread-safe，搭配 Timer 輪詢更新 UI |
| 編碼處理 | OEM Code Page 偵測 | 自動偵測系統 OEM 編碼（如 cp950） |
| 停止機制 | `CancellationToken` + `Task.Delay` | 可即時中斷，不會卡在 sleep |
| 安全保護 | `finally` 區塊自動 re-enable | 確保裝置不會被遺留在停用狀態 |

## 注意事項

- 測試期間目標 USB 裝置會被反覆停用/啟用，**請勿選擇系統關鍵裝置**（如開機碟、系統鍵盤滑鼠）
- 若程式異常終止導致裝置停留在停用狀態，可在「裝置管理員」中手動啟用該裝置
- 部分 USB Hub 或 Root Hub 裝置可能不支援透過 pnputil 停用
