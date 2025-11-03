# Deployment Guide

## GitHub Pages Deployment

This project is configured to automatically deploy to GitHub Pages using **official GitHub Actions** following Microsoft's recommended practices for .NET 9 Blazor WebAssembly applications.

### Setup Steps

1. **Push your code to GitHub** (if you haven't already):
   ```bash
   git add .
   git commit -m "Add GitHub Actions deployment workflow"
   git push origin main
   ```

2. **Enable GitHub Pages** in your repository:
   - Go to your GitHub repository: https://github.com/LuisMars/forbidden-psalm
   - Click on **Settings** (top right)
   - In the left sidebar, click **Pages**
   - Under "Build and deployment":
     - **Source**: Select "GitHub Actions"
     - Click **Save**

   > **Note:** We're using GitHub Actions as the source (not a branch), because the workflow uses the official `actions/deploy-pages` action.

3. **Wait for the workflow to run**:
   - Go to the **Actions** tab in your repository
   - You'll see the "Deploy to GitHub Pages" workflow running
   - Wait for it to complete (takes about 2-3 minutes)

4. **Access your site**:
   - Your app will be available at: **https://luismars.github.io/forbidden-psalm/**
   - It may take a few minutes for the site to become available after the first deployment

### How it Works

The workflow (`.github/workflows/deploy.yml`) uses **official GitHub Actions** and automatically:
1. Checks out your code
2. Installs .NET 9 SDK (using `actions/setup-dotnet@v5`)
3. Publishes the Blazor WebAssembly project in Release configuration
4. Updates the base path from `/` to `/forbidden-psalm/` (required for GitHub Pages subdirectories)
5. Adds a `.nojekyll` file (tells GitHub not to process as Jekyll site, which would skip `_framework` folder)
6. Copies `index.html` to `404.html` (enables client-side SPA routing)
7. Uploads the build artifact (using `actions/upload-pages-artifact@v3`)
8. Deploys to GitHub Pages (using `actions/deploy-pages@v4`)

### Key Configuration Files

#### `.gitattributes`
Contains `*.js binary` to prevent Git from converting line endings in JavaScript files, which is **critical** for Blazor's integrity checks to pass during deployment.

#### `.github/workflows/deploy.yml`
- Uses official GitHub Actions recommended by Microsoft
- Sets proper permissions (`pages: write`, `id-token: write`)
- Implements concurrency control to prevent deployment conflicts
- Targets the `github-pages` environment

### Automatic Updates

Every time you push to the `main` or `master` branch, the workflow will automatically:
- Build your latest changes
- Deploy to GitHub Pages
- Update your live site

### Optional: Enable AOT Compilation (Advanced)

For better runtime performance, you can enable Ahead-of-Time (AOT) compilation. This will:
- ✅ Improve runtime performance (faster execution)
- ❌ Increase build time significantly (10-15 minutes instead of 2-3)
- ❌ Increase initial download size (~2-3x larger)

To enable AOT compilation:

1. **Update your `.csproj` file** (`ForbiddenPsalmBuilder.Blazor.csproj`):
   ```xml
   <PropertyGroup>
     <RunAOTCompilation>true</RunAOTCompilation>
   </PropertyGroup>
   ```

2. **Update the workflow** (`.github/workflows/deploy.yml`), add this step before publishing:
   ```yaml
   - name: Install wasm-tools
     run: dotnet workload install wasm-tools
   ```

> **Recommendation:** Start without AOT to ensure everything works, then enable it later if you need the performance boost.

### Testing Locally

To test the production build locally:
```bash
dotnet publish ForbiddenPsalmBuilder/ForbiddenPsalmBuilder.Blazor/ForbiddenPsalmBuilder.Blazor.csproj -c Release
cd ForbiddenPsalmBuilder/ForbiddenPsalmBuilder.Blazor/bin/Release/net9.0/publish/wwwroot
python3 -m http.server 8080
# Open http://localhost:8080 in your browser
```

### Troubleshooting

**If the workflow fails:**
- Check the **Actions** tab for detailed error messages
- Ensure you selected "GitHub Actions" as the Pages source (not "Deploy from a branch")
- Verify all tests pass locally: `dotnet test`
- Check that `.gitattributes` file exists with `*.js binary`

**If the site doesn't load:**
- Verify GitHub Pages is enabled and source is set to "GitHub Actions"
- Check that the workflow completed successfully in the Actions tab
- Wait 2-5 minutes for GitHub's CDN to propagate changes
- Check the environment URL in Actions → latest run → deploy-to-github-pages

**If CSS/JS files don't load (404 errors):**
- Check browser console for specific 404 errors
- Verify the base href in the deployed `index.html` is `/forbidden-psalm/`
- Confirm `.nojekyll` file exists in the deployed site
- Clear browser cache and try in incognito mode

**If you get integrity check errors:**
- Ensure `.gitattributes` contains `*.js binary`
- Re-commit and push after adding `.gitattributes`
- The `*.js binary` setting prevents line-ending conversions that break integrity hashes

**Reference Documentation:**
- [Official Microsoft Guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/github-pages?view=aspnetcore-9.0)
- [GitHub Pages with Actions](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site#publishing-with-a-custom-github-actions-workflow)
