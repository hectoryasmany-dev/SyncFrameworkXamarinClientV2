﻿using DevExpress.ExpressApp.Security;
using DevExpress.Xpo;
using Orm.Model;
using SyncFrameworkXamarinClientV2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SyncFrameworkXamarinClientV2.ViewModels
{
    public class ItemDetailViewModel : BaseViewModel
    {
        bool readOnly;
        bool canDelete;
        bool canReadDepartment;
        List<Department> departments;
        bool isNewItem;
        bool canWriteDepartment;

        public ItemDetailViewModel(Guid? Oid, INavigation navigation) : base(navigation)
        {
            IsNewItem = (Oid == null);
            if (isNewItem)
            {
                Item = new Employee(uow) { FirstName = "First name", LastName = "Last Name" };
            }
            else
            {
                Item = uow.GetObjectByKey<Employee>(Oid);
            }
            Title = Item?.FullName;
            Departments = uow.Query<Department>().ToList();
            CommandDelete = new Command(async () => {
                await DeleteItemAndGoBack();
            },
        () => CanDelete && !isNewItem);
            CommandUpdate = new Command(async () => {
                await SaveItemAndGoBack();
            },
        () => !ReadOnly);
            CanDelete = XpoHelper.Security.CanDelete(Item);
            ReadOnly = !XpoHelper.Security.CanWrite(Item);
            CanReadDepartment = XpoHelper.Security.CanRead(Item, "Department");
            CanWriteDepartment = XpoHelper.Security.CanWrite(Item, "Department");
            if (isNewItem && CanWriteDepartment)
            {
                Item.Department = Departments?[0];
            }
        }
        async Task DeleteItemAndGoBack()
        {
            uow.Delete(Item);
            await uow.CommitChangesAsync();
            await Navigation.PopToRootAsync();
        }
        async Task SaveItemAndGoBack()
        {
            uow.Save(Item);
            await uow.CommitChangesAsync();
            await Navigation.PopToRootAsync();
        }
        public bool CanDelete
        {
            get { return canDelete; }
            set { SetProperty(ref canDelete, value); CommandDelete.ChangeCanExecute(); }
        }
        public bool CanReadDepartment
        {
            get { return canReadDepartment; }
            set { SetProperty(ref canReadDepartment, value); }
        }
        public bool CanWriteDepartment
        {
            get { return canWriteDepartment; }
            set { SetProperty(ref canWriteDepartment, value); }
        }
        public bool ReadOnly
        {
            get { return readOnly; }
            set { SetProperty(ref readOnly, value); CommandUpdate.ChangeCanExecute(); }
        }
        public Employee Item { get; set; }
        public List<Department> Departments
        {
            get { return departments; }
            set { SetProperty(ref departments, value); }
        }
        public bool IsNewItem
        {
            get { return isNewItem; }
            set { SetProperty(ref isNewItem, value); }
        }
        public Command CommandDelete { get; private set; }
        public Command CommandUpdate { get; private set; }
    }
}
