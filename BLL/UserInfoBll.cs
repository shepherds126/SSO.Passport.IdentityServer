﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Masuit.Tools;
using Masuit.Tools.DateTimeExt;
using Masuit.Tools.Security;
using Masuit.Tools.Win32;
using Models.Dto;
using Models.Entity;

namespace BLL
{
    public partial class UserInfoBll
    {
        /// <summary>
        /// 根据用户名获取
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UserInfo GetByUsername(string name)
        {
            return GetFirstEntity(u => u.Username.Equals(name));
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public UserInfoOutputDto Login(string username, string password)
        {
            UserInfo userInfo = GetByUsername(username);
            if (userInfo != null)
            {
                string key = userInfo.SaltKey;
                string pwd = userInfo.Password;
                password = password.MDString2(key);
                if (pwd.Equals(password))
                {
                    return userInfo.Mapper<UserInfoOutputDto>();
                }
            }
            return null;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public UserInfoOutputDto Register(UserInfo userInfo)
        {
            UserInfo exist = GetFirstEntity(u => u.Username.Equals(userInfo.Username) || u.Email.Equals(userInfo.Email) || u.PhoneNumber.Equals(userInfo.PhoneNumber));
            if (exist is null)
            {
                userInfo.Id = Guid.NewGuid();
                var salt = $"{new Random().StrictNext()}{DateTime.Now.GetTotalMilliseconds()}".MDString2(Guid.NewGuid().ToString()).Base64Encrypt();
                userInfo.Password = userInfo.Password.MDString2(salt);
                userInfo.SaltKey = salt;
                UserInfo added = AddEntity(userInfo);
                int line = SaveChanges();
                return line > 0 ? added.Mapper<UserInfoOutputDto>() : null;
            }
            return null;
        }

        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool UsernameExist(string name)
        {
            UserInfo userInfo = GetByUsername(name);
            return userInfo != null;
        }

        /// <summary>
        /// 检查邮箱是否存在
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool EmailExist(string email) => GetFirstEntityNoTracking(u => u.Email.Equals(email)) != null;

        /// <summary>
        /// 检查电话号码是否存在
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool PhoneExist(string num) => GetFirstEntityNoTracking(u => u.PhoneNumber.Equals(num)) != null;

        /// <summary>
        /// 获取权限列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IList<Control> GetPermissionList(Guid id)
        {
            var user = GetFirstEntity(u => u.Id.Equals(id));
            List<Control> list = new List<Control>(); //所有允许的权限
            if (user != null)
            {
                //1.0 用户-角色-权限-功能 主线，权限的优先级最低
                user.Role.ForEach(r => r.Permission.ForEach(p => list.AddRange(p.Controls.Where(c => c.IsAvailable))));

                //2.0 用户-用户组-角色-权限，权限的优先级其次
                user.UserGroup?.ForEach(g => g.UserGroupPermission.ForEach(ugp =>
                {
                    if (ugp.HasRole)
                    {
                        ugp.Role.Permission.ForEach(p => list.AddRange(p.Controls.Where(c => c.IsAvailable)));
                    }
                    else
                    {
                        ugp.Role.Permission.ForEach(p => list = list.Except(p.Controls).ToList());
                    }
                }));

                //3.0 用户-权限-功能 临时权限，权限的优先级最高
                user.UserPermission?.ForEach(p =>
                {
                    if (p.HasPermission)
                    {
                        list.AddRange(p.Permission.Controls.Where(c => c.IsAvailable));
                    }
                    else
                    {
                        list = list.Except(p.Permission.Controls).ToList();
                    }
                });
            }
            return list.Where(c => c.IsAvailable).Distinct(new FunctionComparision()).ToList();
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="name">用户名，邮箱或者电话号码</param>
        /// <param name="oldPwd">旧密码</param>
        /// <param name="newPwd">新密码</param>
        /// <returns></returns>
        public bool ChangePassword(string name, string oldPwd, string newPwd)
        {
            UserInfo userInfo = GetByUsername(name);
            if (userInfo != null)
            {
                string key = userInfo.SaltKey;
                string pwd = userInfo.Password;
                oldPwd = oldPwd.MDString2(key);
                if (pwd.Equals(oldPwd))
                {
                    var salt = $"{new Random().StrictNext()}{DateTime.Now.GetTotalMilliseconds()}".MDString2(Guid.NewGuid().ToString()).Base64Encrypt();
                    userInfo.Password = newPwd.MDString2(salt);
                    userInfo.SaltKey = salt;
                    return SaveChanges() > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="id">用户id</param>
        /// <param name="oldPwd">旧密码</param>
        /// <param name="newPwd">新密码</param>
        public bool ChangePassword(Guid id, string oldPwd, string newPwd)
        {
            UserInfo userInfo = GetById(id);
            if (userInfo != null)
            {
                string key = userInfo.SaltKey;
                string pwd = userInfo.Password;
                oldPwd = oldPwd.MDString2(key);
                if (pwd.Equals(oldPwd))
                {
                    var salt = $"{new Random().StrictNext()}{DateTime.Now.GetTotalMilliseconds()}".MDString2(Guid.NewGuid().ToString()).Base64Encrypt();
                    userInfo.Password = newPwd.MDString2(salt);
                    userInfo.SaltKey = salt;
                    return SaveChanges() > 0;
                }
            }
            return false;
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <returns></returns>
        public bool ResetPassword(string name, string newPwd = "123456")
        {
            UserInfo userInfo = GetByUsername(name);
            if (userInfo != null)
            {
                var salt = $"{new Random().StrictNext()}{DateTime.Now.GetTotalMilliseconds()}".MDString2(Guid.NewGuid().ToString()).Base64Encrypt();
                userInfo.Password = newPwd.MDString2(salt);
                userInfo.SaltKey = salt;
                return SaveChanges() > 0;
            }
            return false;
        }
    }

    public class FunctionComparision : IEqualityComparer<Control>
    {
        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        public bool Equals(Control x, Control y)
        {
            return x.Controller.Equals(y.Controller) && x.Action.Equals(y.Action);
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <returns>A hash code for the specified object.</returns>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj" /> is a reference type and <paramref name="obj" /> is null.</exception>
        public int GetHashCode(Control obj)
        {
            return 0;
        }
    }
}