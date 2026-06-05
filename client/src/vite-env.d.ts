/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** ApiGateway — jedini javni backend URL (YARP reverse proxy). */
  readonly VITE_API_BASE_URL?: string;
}
