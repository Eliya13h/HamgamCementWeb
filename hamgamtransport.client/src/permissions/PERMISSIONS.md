# سیستم دسترسی (Permissions)

این سند مرجع اصلی برای افزودن، استفاده و نگهداری دسترسی‌ها در پروژه HamgamTransport است.

## ایده کلی

- دسترسی‌ها **اجازه‌محور** و **مستقیم روی کاربر** اعمال می‌شوند.
- **نقش (Role)** فقط نمادین است (مثل «مدیر»، «کارمند») و هیچ محدودیت دسترسی ندارد.
- هر کاربر جدید به‌صورت پیش‌فرض **`HasFullAccess = true`** دارد.
- صفحه **سطح دسترسی** (`AccessLevelsPage`) برای ویرایش دسترسی هر کاربر است.

## ساختار درخت (۳ سطح)

| سطح | منبع | مثال کلید | توضیح |
|-----|------|-----------|--------|
| ۱ – ماژول | آیتم گروهی سایدبار | `people` | فقط برای نمایش درخت |
| ۲ – صفحه | زیرمنوی سایدبار | `people.customers` | گروه عملیات |
| ۳ – عملیات | برگ درخت | `people.customers.view` | **در جدول `UserPermissions` ذخیره می‌شود** |

## فایل‌های مهم

| فایل | نقش |
|------|-----|
| `src/config/navigation.js` | منبع صفحات سایدبار |
| `src/permissions/registry.js` | ساخت خودکار درخت |
| `src/permissions/pageActions.js` | عملیات اضافهٔ هر صفحه (غیر از CRUD) |
| `src/permissions/usePageCrud.js` | هوک `canCreate` / `canEdit` / … |
| `src/Pages/Users/AccessLevelsPage.jsx` | مدیریت دسترسی کاربران |
| `HamgamTransport.Server/.../User.cs` | `HasFullAccess` روی کاربر |
| `HamgamTransport.Server/.../UserPermission.cs` | کلیدهای عملیات |

| `src/permissions/usePermission.js` | هوک `can()` |

## عملیات CRUD در درخت

زیر هر صفحه (مشتریان، کارمندان، …) چهار عملیات استاندارد نمایش داده می‌شود:
`مشاهده` · `ایجاد` · `ویرایش` · `حذف`

عملیات خاص هر صفحه در `pageActions.js` تعریف می‌شود.

## افزودن عملیات جدید به یک صفحه

1. صفحه را در `navigation.js` و `AppRoutes.jsx` اضافه کنید.
2. در UI از `Can` یا `usePermission` استفاده کنید.
3. در صفحه سطح دسترسی، درخت خودکار به‌روز می‌شود.

```jsx
import { Can, pathPermission } from '../../permissions'

<Can permission={pathPermission('/people/customers', 'create')}>
  <button>مشتری جدید</button>
</Can>
```

## API

| متد | مسیر | توضیح |
|-----|------|--------|
| GET | `/api/users/{id}/permissions` | خواندن دسترسی کاربر |
| PUT | `/api/users/{id}/permissions` | ذخیره دسترسی کاربر |
| GET | `/api/auth/me` | شامل `hasFullAccess` و `permissions` |

## نکات

- نقش ≠ دسترسی؛ نقش فقط در پروفایل و گزارش‌ها نمایش داده می‌شود.
- کاربر جدید = دسترسی کامل تا مدیر محدودش کند.
- فقط کلیدهای سطح ۳ (عملیات) در دیتابیس ذخیره می‌شوند.
