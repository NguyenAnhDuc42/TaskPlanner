import { ThemeProvider } from "@/components/theme-provider";

export default function MainLayout({ children,}: Readonly<{ children: React.ReactNode;}>)  {
    return(
        <>
             <ThemeProvider attribute="class" defaultTheme="dark"
                enableSystem
                disableTransitionOnChange
                >
                {children}
            </ThemeProvider>
        </>
    )
};
