﻿//-----------------------------------------------------------------
// All Rights Reserved , Copyright (C) 2012 , Hairihan TECH, Ltd .
//-----------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using DotNet.BaseManagers;
using DotNet.Manager.Permission;
using DotNet.Manager.User;
using DotNet.Model;
using DotNet.Utilities.Common;

namespace DotNet.Manager.Common
{
    /// <summary>
    /// BaseTableColumnsManager
    /// 表字段定义
    ///
    /// 修改纪录
    ///
    ///		2012-02-06 版本：1.1 JiRiGaLa 把字段权限整理完善。
    ///		2010-07-14 版本：1.0 JiRiGaLa 创建主键。
    ///
    /// 版本：1.0
    ///
    /// <author>
    ///		<name>JiRiGaLa</name>
    ///		<date>2010-07-14</date>
    /// </author>
    /// </summary>
    public partial class BaseTableColumnsManager : BaseManager, IBaseManager
    {
        /// <summary>
        /// 获取用户的列权限
        /// </summary>
        /// <param name="tableCode">表名</param>
        /// <param name="permissionCode">操作权限</param>
        /// <returns>有权限的列数组</returns>
        public string[] GetColumns(string tableCode, string permissionCode = "Column.Access")
        {
            return GetColumns(this.UserInfo.Id, tableCode, permissionCode);
        }

        /// <summary>
        /// 获取用户的列权限
        /// </summary>
        /// <param name="userId">用户主键</param>
        /// <param name="tableCode">表名</param>
        /// <param name="permissionCode">操作权限</param>
        /// <returns>有权限的列数组</returns>
        public string[] GetColumns(string userId, string tableCode, string permissionCode = "Column.Access")
        {
            // Column.Edit
            string[] returnValue = null;
            if (permissionCode.Equals("Column.Deney") || permissionCode.Equals("Column.Edit"))
            {
                // 按数据权限来过滤数据
                BasePermissionScopeManager permissionScopeManager = new BasePermissionScopeManager(DbHelper, UserInfo);
                returnValue = permissionScopeManager.GetResourceScopeIds(userId, tableCode, permissionCode);
            }
            else if (permissionCode.Equals("Column.Access"))
            {
                // 1: 用户有权限的列名
                BasePermissionScopeManager permissionScopeManager = new BasePermissionScopeManager(DbHelper, UserInfo);
                returnValue = permissionScopeManager.GetResourceScopeIds(userId, tableCode, permissionCode);
                // 2: 先获取公开的列名
                string[] publicIds = this.GetProperties(new KeyValuePair<string, object>(BaseTableColumnsEntity.FieldTableCode, tableCode), new KeyValuePair<string, object>(BaseTableColumnsEntity.FieldIsPublic, 1), BaseTableColumnsEntity.FieldColumnCode);
                returnValue = StringUtil.Concat(returnValue, publicIds);
            }
            return returnValue;
        }

        /// <summary>
        /// 获取能访问的字段列表
        /// </summary>
        /// <param name="tableCode">表名</param>
        /// <returns>数据表</returns>
        public DataTable GetTableColumns(string userId, string tableCode)
        {
            // 当前用户对哪些资源有权限（用户自己的权限 + 相应的角色拥有的权限）
            BasePermissionScopeManager permissionScopeManager = new BasePermissionScopeManager(DbHelper, UserInfo);
            string[] ids = permissionScopeManager.GetResourceScopeIds(userId, "TableColumns", "ColumnAccess");

            // 获取有效的，没删除标志的
            string sqlQuery = " SELECT * FROM BaseTableColumns WHERE (DeletionStateCode = 0 AND Enabled = 1) ";
            
            // 是否指定了表名
            if (!string.IsNullOrEmpty(tableCode))
            {
                sqlQuery += " AND (TableCode = '" + tableCode + "') ";
            }

            // 公开的或者按权限来过滤字段
            sqlQuery += " AND (IsPublic = 1 ";
            if (ids != null && ids.Length > 0)
            {
                string idList = StringUtil.ArrayToList(ids);
                sqlQuery += " OR Id IN (" + idList + ")";
            }
            sqlQuery += ") ORDER BY SortCode ";

            return DbHelper.Fill(sqlQuery);
        }

        /// <summary>
        /// 获取约束条件（所有的约束）
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="resourceCategory">资源类别</param>
        /// <param name="resourceId">资源主键</param>
        /// <returns>数据表</returns>
        public DataTable GetConstraintDT(string resourceCategory, string resourceId, string permissionCode = "Resource.AccessPermission")
        {
            DataTable dataTable = new DataTable(BaseTableColumnsEntity.TableName);

            /*
            -- 这里是都有哪些表？
            SELECT ItemValue, ItemName
            FROM ItemsTablePermissionScope
            WHERE (DeletionStateCode = 0) 
            AND (Enabled = 1)
            ORDER BY ItemsTablePermissionScope.SortCode
             */

            /*
            -- 这里是都有有哪些表达式
            SELECT     Id, TargetId, PermissionConstraint   -- 对什么表有什么表达式？
            FROM         BasePermissionScope
            WHERE (ResourceId = 10000000) 
            AND (ResourceCategory = 'BaseRole')   -- 什么角色？
            AND (TargetId = 'BaseUser') 
            AND (TargetCategory = 'Table') 
            AND (PermissionId = 10000001)  -- 有什么权限？（资源访问权限）
            AND (DeletionStateCode = 0) 
            AND (Enabled = 1)
             */

            string permissionId = string.Empty;
            BasePermissionItemManager permissionItemManager = new BasePermissionItemManager(this.UserInfo);
            permissionId = permissionItemManager.GetIdByAdd(permissionCode);

            string sqlQuery = @" SELECT BasePermissionScope.Id
		                                    , ItemsTablePermissionScope.ItemValue AS TableCode
		                                    , ItemsTablePermissionScope.ItemName AS TableName
		                                    , BasePermissionScope.PermissionConstraint
		                                    , ItemsTablePermissionScope.SortCode
                                    FROM  (
	                                    SELECT ItemValue
		                                     , ItemName
		                                     , SortCode
	                                    FROM ItemsTablePermissionScope
                                       WHERE (DeletionStateCode = 0) 
		                                      AND (Enabled = 1)                                              
                                        ) AS ItemsTablePermissionScope LEFT OUTER JOIN
                                        (SELECT Id
			                                    , TargetId
			                                    , PermissionConstraint  
                                           FROM BasePermissionScope
                                         WHERE (ResourceCategory = '" + resourceCategory + @"') 
			                                    AND (ResourceId = " + resourceId + @") 
			                                    AND (TargetCategory = 'Table') 
			                                    AND (PermissionId = " + permissionId.ToString() + @") 
			                                    AND (DeletionStateCode = 0) 
			                                    AND (Enabled = 1)
	                                     ) AS BasePermissionScope 
                                    ON ItemsTablePermissionScope.ItemValue = BasePermissionScope.TargetId
                                    ORDER BY ItemsTablePermissionScope.SortCode ";

            dataTable = this.Fill(sqlQuery);
            dataTable.TableName = BaseTableColumnsEntity.TableName;

            return dataTable;
        }


        /// <summary>
        /// 获取用户的件约束表达式
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="tableName">表名</param>
        /// <returns>主键</returns>
        public string GetUserConstraint(string tableName, string permissionCode = "Resource.AccessPermission")
        {
            string returnValue = string.Empty;
            // 这里是获取用户的条件表达式
            // 1: 首先用户在哪些角色里是有效的？
            // 2: 这些角色都有哪些哪些条件约束？
            // 3: 组合约束条件？
            // 4：用户本身的约束条件？
            string permissionId = string.Empty;
            BasePermissionItemManager permissionItemManager = new BasePermissionItemManager(this.UserInfo);
            permissionId = permissionItemManager.GetIdByAdd(permissionCode);

            BaseUserManager manager = new BaseUserManager(this.DbHelper, this.UserInfo);
            string[] roleIds = manager.GetAllRoleIds(UserInfo.Id);
            if (roleIds == null || roleIds.Length == 0)
            {
                return returnValue;
            }
            BasePermissionScopeManager scopeManager = new BasePermissionScopeManager(this.DbHelper, this.UserInfo);

            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceCategory, BaseRoleEntity.TableName));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceId, roleIds));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetCategory, "Table"));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetId, tableName));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldPermissionItemId, permissionId));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldEnabled, 1));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldDeletionStateCode, 0));

            DataTable dtPermissionScope = scopeManager.GetDataTable(parameters);
            string permissionConstraint = string.Empty;
            foreach (DataRow dataRow in dtPermissionScope.Rows)
            {
                permissionConstraint = dataRow[BasePermissionScopeEntity.FieldPermissionConstraint].ToString();
                permissionConstraint = permissionConstraint.Trim();
                if (!string.IsNullOrEmpty(permissionConstraint))
                {
                    returnValue += " AND " + permissionConstraint;
                }
            }
            if (!string.IsNullOrEmpty(returnValue))
            {
                returnValue = returnValue.Substring(5);
                // 解析替换约束表达式标准函数
                returnValue = ConstraintUtil.PrepareParameter(this.UserInfo, returnValue);
            }

            return returnValue;
        }

        /// <summary>
        /// 设置约束条件
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="resourceCategory">资源类别</param>
        /// <param name="resourceId">资源主键</param>
        /// <param name="tableName">表名</param>
        /// <param name="constraint">约束</param>
        /// <param name="enabled">有效</param>
        /// <param name="permissionCode">操作权限项</param>
        /// <returns>主键</returns>
        public string SetConstraint(string resourceCategory, string resourceId, string tableName, string permissionCode, string constraint, bool enabled = true)
        {
            string returnValue = string.Empty;

            string permissionId = string.Empty;
            BasePermissionItemManager permissionItemManager = new BasePermissionItemManager(this.UserInfo);
            permissionId = permissionItemManager.GetIdByAdd(permissionCode);

            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceCategory, resourceCategory));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceId, resourceId));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetCategory, "Table"));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetId, tableName));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldPermissionItemId, permissionId));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldDeletionStateCode, 0));

            BasePermissionScopeManager manager = new BasePermissionScopeManager(this.DbHelper, this.UserInfo);
            // 1:先获取是否有这样的主键，若有进行更新操作。
            // 2:若没有进行添加操作。
            returnValue = manager.GetId(parameters);
            if (!string.IsNullOrEmpty(returnValue))
            {
                parameters = new List<KeyValuePair<string, object>>();
                parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldPermissionConstraint, constraint));
                parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldEnabled, enabled ? 1 : 0));
                manager.SetProperty(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldId, returnValue), parameters);
            }
            else
            {
                BasePermissionScopeEntity entity = new BasePermissionScopeEntity();
                entity.ResourceCategory = resourceCategory;
                entity.ResourceId = resourceId;
                entity.TargetCategory = "Table";
                entity.TargetId = tableName;
                entity.PermissionConstraint = constraint;
                entity.PermissionId = int.Parse(permissionId);
                entity.DeletionStateCode = 0;
                entity.Enabled = enabled ? 1: 0;
                returnValue = manager.Add(entity);
            }
            return returnValue;
        }

        /// <summary>
        /// 获取约束条件
        /// </summary>
        /// <param name="userInfo">用户</param>
        /// <param name="resourceCategory">资源类别</param>
        /// <param name="resourceId">资源主键</param>
        /// <param name="tableName">表名</param>
        /// <returns>约束条件</returns>
        public string GetConstraint(string resourceCategory, string resourceId, string tableName, string permissionCode = "Resource.AccessPermission")
        {
            string returnValue = string.Empty;
            BasePermissionScopeEntity entity = GetConstraintEntity(resourceCategory, resourceId, tableName, permissionCode);
            if (entity != null)
            {
                if (entity.Enabled == 1)
                {
                    returnValue = entity.PermissionConstraint;
                }
            }
            return returnValue;
        }

        public BasePermissionScopeEntity GetConstraintEntity(string resourceCategory, string resourceId, string tableName, string permissionCode = "Resource.AccessPermission")
        {
            BasePermissionScopeEntity entity = null;

            string permissionId = string.Empty;
            BasePermissionItemManager permissionItemManager = new BasePermissionItemManager(this.UserInfo);
            permissionId = permissionItemManager.GetIdByAdd(permissionCode);

            List<KeyValuePair<string, object>> parameters = new List<KeyValuePair<string, object>>();
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceCategory, resourceCategory));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldResourceId, resourceId));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetCategory, "Table"));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldTargetId, tableName));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldPermissionItemId, permissionId));
            parameters.Add(new KeyValuePair<string, object>(BasePermissionScopeEntity.FieldDeletionStateCode, 0));

            // 1:先获取是否有这样的主键，若有进行更新操作。
            BasePermissionScopeManager manager = new BasePermissionScopeManager(this.DbHelper, this.UserInfo);
            DataTable dt = manager.GetDataTable(parameters);
            if (dt.Rows.Count > 0)
            {
                entity = new BasePermissionScopeEntity(dt);
            }
            return entity;
        }
    }
}