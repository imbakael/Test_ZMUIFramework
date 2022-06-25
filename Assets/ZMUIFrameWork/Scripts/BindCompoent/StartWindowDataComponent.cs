/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Author:铸梦
 *Date:2022/6/25 18:05:31
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，任何手动修改都会被下次生成覆盖,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;

namespace ZMUIFrameWork
{
	public class StartWindowDataComponent:MonoBehaviour
	{
		public   Button  StartGameButton;

		public   Button  QuitGameButton;

		public   Button  SettingButton;

		public   Button  Setting1Button;

		public  void InitComponent(WindowBase target)
		{
		     //组件事件绑定
		     StartWindow mWindow=(StartWindow)target;
		     target.AddButtonClickListener(StartGameButton,mWindow.OnStartGameButtonClick);
		     target.AddButtonClickListener(QuitGameButton,mWindow.OnQuitGameButtonClick);
		     target.AddButtonClickListener(SettingButton,mWindow.OnSettingButtonClick);
		     target.AddButtonClickListener(Setting1Button,mWindow.OnSetting1ButtonClick);
		}
	}
}
