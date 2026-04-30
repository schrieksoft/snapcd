These pages all use static SSR:
1. in order to be able to set cookies with SignInManager. (Interactive SSR does not have access to cookies during rendering)
2. In order to make use of IdentityRedirectManager, which does not work in interactive SSR
3. Certain UserManager actions fail if _user created with one dbContext and then updated later with another one. But this means we need to have one dbContext per scope, which is only possible is static pages (in interactive pages we need to use the factory pattern)