# 网站部署与回退

这里是 Ubuntu / Nginx 部署模板，不是某台服务器的操作记录。所有配置中的 `example.com` 都必须先替换为自己的域名。主机地址、SSH 主机指纹、账户信息和实际部署日志由维护者另行保管，不能提交到公开仓库。

## 文件与目录

| 位置 | 用途 |
| --- | --- |
| `/srv/tomato-focus/releases/<发布编号>` | 不可覆盖的静态版本，目录 0755、文件 0644 |
| `/srv/tomato-focus/current`、`previous` | 当前与上一个版本的符号链接 |
| `/etc/nginx/tomato-focus/` | 本站配置模板 |
| `/usr/local/lib/tomato-focus/` | 发布、校验与 HTTPS 脚本 |
| `/var/lib/tomato-focus/incoming/` | 私有上传区，目录 0700 |
| `/var/lib/tomato-focus/operations/` | 私有发布记录 |
| `/var/lib/tomato-focus/acme/` | HTTP 证书验证文件 |
| `/var/log/tomato-focus/` | 本站日志 |

Nginx 工作进程只读站点文件；发布由受控管理员账户完成。首次安装前创建上述目录，将配置和脚本放到相应位置；安装 Nginx、Certbot、Python 3 和 curl。保留服务器上其他站点、默认配置与系统服务。

## 先验收，再启用 HTTPS

1. 在 Windows 运行 `./scripts/build-website.ps1`，确认桌面包、安装器与网站清单校验通过。
2. 将网站 ZIP 上传到 `incoming/`，用本地 `.sha256` 中的值校验。不要上传整个仓库。
3. 设置 `staging.conf` 为本站启用配置，使用 `nginx -t` 检查后重载。内部完整站点只监听 `127.0.0.1:8080`；公网 HTTP 只开放本站 ACME 路径，其余请求返回 503。
4. 发布后执行 `verify-site.py`，检查内容哈希、下载、安全头、断点续传与私有文件隔离。首次发布可在 Nginx 启动前解包，启动后必须补做完整校验。
5. 核对域名解析、80/443 入口和适用的域名接入要求；使用正式域名签发可信证书。证书路径与配置保持一致。
6. 执行 `activate-https.sh`。它检查证书并切换到生产配置；健康检查失败会回退配置。随后从公网确认 HTTPS、HTTP 跳转和实际下载，并验证续期流程。

模板不会自动配置 DNS、云安全组或备案信息。`renew-hook.sh` 仅针对本站证书重载 Nginx；`logrotate.conf` 管理本站日志。

## 发布和回退

```sh
python3 /usr/local/lib/tomato-focus/deploy-release.py /var/lib/tomato-focus/incoming/NEW.zip NEW_RELEASE_ID EXPECTED_SHA256
python3 /usr/local/lib/tomato-focus/verify-site.py
```

发布前检查 ZIP 哈希、条目白名单、路径与逐文件哈希，再创建新版本并原子切换 `current`。主页校验失败恢复先前链接，成功后记录 `previous` 和发布日志。发布编号必须唯一，脚本不覆盖旧版本。

回退前确认目标是 `/srv/tomato-focus/releases/` 内已验收的版本，使用临时符号链接原子替换 `current`，然后重新执行站点校验。首次发布没有前一版本，不可假设 `previous` 存在。暂停站点只停用本站配置，不停止整台服务器的 Nginx。

仓库中的自动检查只证明构建与打包结果；服务器权限、证书、真实域名和公网行为需要在部署环境单独验收。
