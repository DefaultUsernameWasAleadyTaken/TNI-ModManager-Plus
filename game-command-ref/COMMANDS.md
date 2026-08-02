# Terminal commands

In-game terminal routines. For alias authoring / autocomplete.

Total: **33**.

| Command | Summary | Primary usage | Needs on | Needs using |
|---|---|---|---|---|
| `alias` | use the terminal your way. | `alias` | False | False |
| `always` | shell quality-of-life enhancements. | `always using <debugger_addr>` | False | False |
| `botconf` | manage bots on devices. |  | True | True |
| `clear` | clear screen. |  | False | False |
| `cron` | scheduled commands. |  | False | False |
| `dhcp` | show DHCP options on dhcp server. | `dhcp show on <dhcp_srv_addr> using <debugger_addr>` | True | True |
| `dns` | test resolving a domain. | `dns lookup <domain> [on <dns_addr>] using <debugger_addr>` | False | True |
| `dstat` | view device status through a remote debugger. | `dstat <address> using <debugger_address>` | False | True |
| `echo` | send multiplayer message (visible on everyone's terminal). | `echo <msg>` | False | False |
| `firewall` | manage rules on firewalls. | `firewall show on <firewall_addr> using <debugger_addr>` | True | True |
| `god` | pressing UP arrow will cycle previous commands. |  | False | False |
| `haconf` | manage high-availability setup on devices. | `haconf show on <dev_addr> using <debugger_addr>` | True | True |
| `lstdbg` | list debuggers for remote access. | `lstdbg` | False | False |
| `man` | usage manual/help routine. |  | False | False |
| `middlebox` | manage middlebox configuration on devices. |  | True | True |
| `net` | network address must begin with a '@' and may only consist of digits 0-9, alphabets a-z, '_' underscore, '-' dash and '/' slash characters. | `net timeout request set <seconds> on <target_addr> using <debugger_addr>` | True | True |
| `notify` | sends an on-screen notification. | `notify <msg...>` | False | False |
| `pcap` | starts traffic capture on a network tap. | `pcap [dump] [exclude] [=<traffic_type>] on <tap_address> using <debugger_address>` | True | True |
| `ping` | ping a device from the debugger. | `ping <address1> using <debugger_address>` | False | True |
| `power` | remote device power management. | `power wake\|suspend [on <dev_addr>\|broadcast]` | True | True |
| `program` | list available programs to install. | `program list` | True | True |
| `quit` |  |  | False | False |
| `rip` | configure auto route discovery on routers. | `rip show on <router_addr> using <debugger_addr>` | True | True |
| `route` | manage routes on routers. | `route show on <router_addr> using <debugger_addr>` | True | True |
| `scan` | scan and list devices that are accessible via a broadcast. | `scan [device_type] [noex] [from <source>] [with <traffic_type>] using <debugger_address>` | False | True |
| `sftp` | remote tool for file backup/migration. |  | True | True |
| `sleep` | delay execution for N seconds. |  | False | False |
| `stp` | configure spanning tree protocol on managed switches. | `stp show on <mngedsw> using <debugger_addr>` | True | True |
| `trace` | trace network traversal from a device to another device. | `trace <address1> from <address2> using <debugger_address>` | False | True |
| `try` | try a command, run another if failed. | `try <cmd1> [then <cmd2>] [else <cmd3>]` | False | False |
| `vlan` | manage virtual local area networks. | `vlan show on <msw_addr> using <debugger_addr>` | True | True |
| `vmconf` | manage virtual machines. | `vmconf show on <srv_addr> using <debugger_addr>` | False | False |
| `watch` | watch the device information/programs through a remote debugger. | `watch <address> using <debugger_address>` | False | True |
