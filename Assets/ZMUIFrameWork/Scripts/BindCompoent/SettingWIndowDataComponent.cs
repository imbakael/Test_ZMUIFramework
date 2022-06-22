/*---------------------------------
 *Title:UI自动化组件生成代码生成工具
 *Author:铸梦
 *Date:2022/5/16 22:14:05
 *Description:变量需要以[Text]括号加组件类型的格式进行声明，然后右键窗口物体—— 一键生成UI数据组件脚本即可
 *注意:以下文件是自动生成的，任何手动修改都会被下次生成覆盖,若手动修改后,尽量避免自动生成
---------------------------------*/
using UnityEngine.UI;
using UnityEngine;

namespace ZMUIFrameWork
{
	public class SettingWIndowDataComponent:MonoBehaviour
	{
		public   Button  CloseButton;

		public  void InitComponent(WindowBase target)
		{
		     //组件事件绑定
		     SettingWIndow mWindow=(SettingWIndow)target;
		     target.AddButtonClickListener(CloseButton,mWindow.OnCloseButtonClick);
		}
	}
}
