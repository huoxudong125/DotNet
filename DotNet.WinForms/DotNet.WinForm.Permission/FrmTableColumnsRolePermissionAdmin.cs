﻿//-----------------------------------------------------------
// All Rights Reserved , Copyright (C) 2012 , Hairihan TECH, Ltd .
//-----------------------------------------------------------

using System;
using System.Windows.Forms;
using DotNet.Model;
using DotNet.WinForm.Common;
using DotNet.WinForm.Role;

namespace DotNet.WinForm.Permission
{
    public partial class FrmTableColumnsRolePermissionAdmin : FrmRoleAdmin
    {
        public FrmTableColumnsRolePermissionAdmin()
        {
            InitializeComponent();
        }

        protected override void btnPermission_Click(object sender, EventArgs e)
        {
            // 资源权限调用的参数
            string resourceCategory = BaseRoleEntity.TableName;
            string resourceId = this.TargetRoleId;
            string resourceName = this.TargetRoleRealName;
            string permissionItemCode = "ColumnAccess";
            string targetCategory = "TableColumns";

            string targetSQL = "SELECT Id, TableCode + '.' + ColumnCode AS RealName, TableName + '.' + ColumnName AS Description FROM BaseTableColumns WHERE DeletionStateCode = 0 AND Enabled = 1 ORDER BY SortCode";

            string assemblyName = "DotNet.WinForm.ResourcePermission";
            string formName = "FrmResourcePermission";
            Type assemblyType = CacheManager.Instance.GetType(assemblyName, formName);
            Form frmUserPermissionAdmin = (Form)Activator.CreateInstance(assemblyType, resourceCategory, resourceId, resourceName, permissionItemCode, targetCategory, targetSQL);
            frmUserPermissionAdmin.ShowDialog(this);
        }
    }
}
